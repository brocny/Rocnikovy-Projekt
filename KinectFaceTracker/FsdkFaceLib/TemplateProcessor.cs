using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Face;
using FsdkFaceLib.Properties;

namespace FsdkFaceLib
{
    internal class TemplateProcessor
    {
        private const int ClearCacheIterationsLimit = 20;

        private readonly ConcurrentDictionary<long, TaskCompletionSource<TrackingStatus>> _addTemplates =
            new ConcurrentDictionary<long, TaskCompletionSource<TrackingStatus>>();

        private int _cacheClearingCounter;
        private readonly IFaceDatabase<byte[]> _faceDb;
        private readonly ConcurrentDictionary<long, TrackingStatus> _trackedFaces;

        public TemplateProcessor(IFaceDatabase<byte[]> faceDb,
            IReadOnlyDictionary<long, TrackingStatus> trackedFaces = null)
        {
            _faceDb = faceDb;
            _trackedFaces = trackedFaces == null
                ? new ConcurrentDictionary<long, TrackingStatus>()
                : new ConcurrentDictionary<long, TrackingStatus>(trackedFaces);
        }


        public IReadOnlyDictionary<long, TrackingStatus> TrackedFaces => _trackedFaces;

        public MatchingParameters MatchingParameters { get; set; } = MatchingParameters.Default;

        public Task<TrackingStatus> Capture(long trackingId)
        {
            var tsc = new TaskCompletionSource<TrackingStatus>();
            _addTemplates[trackingId] = tsc;
            return tsc.Task;
        }

        public Match<byte[]> ProcessTemplate(FaceTemplate t)
        {
            if (_addTemplates.TryRemove(t.TrackingId, out var tsc))
            {
                var result = Capture(t);
                tsc.SetResult(result);
                return new Match<byte[]>{ IsValid = false };
            }

            if (!_trackedFaces.TryGetValue(t.TrackingId, out var trackingStatus)) return ProcessUntracked(t);

            trackingStatus.WasSeen = true;
            var topCandidate = trackingStatus.TopCandidate;
            var topCondidateFaceInfo = _faceDb[topCandidate.FaceId];
            var topCandidateMatch = topCondidateFaceInfo.GetSimilarity(t);

            if (topCandidateMatch.similarity > MatchingParameters.TrackedInstantMatchThreshold)
            {
                return Matched(t, topCandidate, topCandidateMatch.similarity, topCandidateMatch.snapshot, topCondidateFaceInfo);
            }

            if (trackingStatus.Candidates.Count > 1)
            {
                var bestOfTheRest = GetBestOfTheRest(trackingStatus, t);
                if (bestOfTheRest.similarity > MatchingParameters.TrackedInstantMatchThreshold)
                {
                    var match = Matched(t, bestOfTheRest.candidate, bestOfTheRest.similarity, bestOfTheRest.snapshot, _faceDb[bestOfTheRest.candidate.FaceId]);
                    if (bestOfTheRest.candidate.Confirmations > topCandidate.Confirmations)
                    {
                        trackingStatus.TopCandidate = bestOfTheRest.candidate;
                    }

                    return match;
                }

                if (bestOfTheRest.similarity > topCandidateMatch.similarity)
                {
                    topCandidate = bestOfTheRest.candidate;
                    topCandidateMatch = (bestOfTheRest.similarity, bestOfTheRest.snapshot);
                }
            }

            if (topCandidateMatch.similarity > MatchingParameters.TrackedNewTemplateThreshold)
            {
                NewTemplate(t, topCandidate);
            }

            if (topCandidateMatch.similarity > MatchingParameters.MatchThreshold)
            {
                return Matched(t, topCandidate, topCandidateMatch.similarity, topCandidateMatch.snapshot, _faceDb[topCandidate.FaceId]);
            }

            return ProcessUntracked(t);
        }

        private Match<byte[]> ProcessUntracked(FaceTemplate t)
        {
            var bestMatch = _faceDb.GetBestMatch(t);
            if (bestMatch == null) return new Match<byte[]>{ IsValid = false };
            bestMatch.TrackingId = t.TrackingId;
            if (bestMatch.Similarity <= MatchingParameters.MatchThreshold)
            {
                bestMatch.IsValid = false;
                return bestMatch;
            }

            _trackedFaces[t.TrackingId] =
                new TrackingStatus(new CandidateStatus {Confirmations = bestMatch.Similarity, FaceId = bestMatch.FaceId});

            return bestMatch;
        }

        public TrackingStatus Capture(FaceTemplate t)
        {
            if (!_trackedFaces.TryGetValue(t.TrackingId, out var ts))
            {
                ts = _trackedFaces[t.TrackingId] = new TrackingStatus(new CandidateStatus {FaceId = _faceDb.NextId, Confirmations = 1});
            }

            NewTemplate(t, ts.TopCandidate);

            return ts;
        }

        private Match<byte[]> Matched(FaceTemplate t, CandidateStatus cs, float confidence, FaceSnapshot<byte[]> snapshot, IFaceInfo<byte[]> faceInfo)
        {
            cs.Confirmations += confidence;
            return new Match<byte[]>(cs.FaceId, confidence, snapshot, faceInfo, t.TrackingId);
        }

        private void NewTemplate(FaceTemplate t, CandidateStatus ts)
        {
            _faceDb.AddOrUpdate(ts.FaceId, t);
        }

        /// <summary>
        ///     Gets the best-matching candidate in <paramref name="ts" /> excluding the <see cref="TrackingStatus.TopCandidate" />
        /// </summary>
        /// <param name="ts">Where to look for candidates</param>
        /// <param name="template">What to match candidates to</param>
        /// <returns>The best-maching <see cref="CandidateStatus" /> and match confidence (between 0 and 1).</returns>
        private (CandidateStatus candidate, float similarity, FaceSnapshot<byte[]> snapshot) 
        GetBestOfTheRest(TrackingStatus ts, IFaceTemplate<byte[]> template)
        {
            (float simimilarity, FaceSnapshot<byte[]> snapshot) bestMatch = default;
            CandidateStatus bestCand = default;
            var candidates = ts.Candidates;
            for (int i = 1; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                var match = _faceDb[candidate.FaceId].GetSimilarity(template);
                if (match.similarity > bestMatch.simimilarity)
                {
                    bestMatch = match;
                    bestCand = candidate;
                }
            }

            return (bestCand, bestMatch.simimilarity, bestMatch.snapshot);
        }

        /// <summary>
        ///     Process multiple templates at once
        /// </summary>
        /// <param name="templates">Templates to be processed</param>
        /// <returns>Any matches that might be found</returns>
        public IEnumerable<Match<byte[]>> ProcessTemplates(IEnumerable<FaceTemplate> templates)
        {
            if (_cacheClearingCounter++ > ClearCacheIterationsLimit)
            {
                foreach (var ts in _trackedFaces.Where(t => !t.Value.WasSeen).ToList())
                {
                    _trackedFaces.TryRemove(ts.Key, out _);
                }

                _cacheClearingCounter = 0;
            }

            return templates
                .Where(t => t?.Template != null)
                .Select(ProcessTemplate)
                .Where(match => match.IsValid && match.Similarity >= MatchingParameters.MatchThreshold)
                .ToArray();
        }
    }

    public class MatchingParameters
    {
        public float TrackedInstantMatchThreshold { get; set; }
        public float TrackedNewTemplateThreshold { get; set; }
        public float MatchThreshold { get; set; }

        public static MatchingParameters Default => new MatchingParameters
        {
            TrackedInstantMatchThreshold = FsdkSettings.Default.TrackedInstantMatchThreshold,
            TrackedNewTemplateThreshold = FsdkSettings.Default.TrackedNewTemplateThreshold,
            MatchThreshold = FsdkSettings.Default.MatchThreshold,
        };
    }
}