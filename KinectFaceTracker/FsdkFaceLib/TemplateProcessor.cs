using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;
using Core.Face;
using FsdkFaceLib.Properties;

namespace FsdkFaceLib
{
    internal class TemplateProcessor
    {
        private const int ClearCacheIterationsLimit = 20;

        private readonly ConcurrentDictionary<long, (TaskCompletionSource<TrackingStatus> tcs, bool forceNewFace)> _addTemplateRequests =
            new ConcurrentDictionary<long, (TaskCompletionSource<TrackingStatus> tsc, bool forceNewFace)>();

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

        public Task<TrackingStatus> Capture(long trackingId, bool forceNewFace = false)
        {
            var tsc = new TaskCompletionSource<TrackingStatus>();
            _addTemplateRequests[trackingId] = (tsc, forceNewFace);
            return tsc.Task;
        }

        public  Match<byte[]> ProcessTemplate(FaceTemplate t)
        {
            if (_addTemplateRequests.TryRemove(t.TrackingId, out var request))
            {
                var result = CaptureImpl(t, request.forceNewFace);
                request.tcs.SetResult(result);
                return new Match<byte[]>{ IsValid = false };
            }

            if (!_trackedFaces.TryGetValue(t.TrackingId, out var trackingStatus)) return ProcessUntracked(t);

            trackingStatus.WasSeen = true;
            var topCandidate = trackingStatus.TopCandidate;
            var topCondidateFaceInfo = _faceDb[topCandidate.FaceId];
            var topCandidateMatch = topCondidateFaceInfo.GetSimilarity(t);

            if (topCandidateMatch.similarity > MatchingParameters.InstantMatchThreshold)
            {
                if (topCondidateFaceInfo.Templates.Count() < 5)
                {
                    AddTemplate(t, topCandidate);
                }

                return Matched(topCandidate, topCandidateMatch.similarity, topCandidateMatch.snapshot, topCondidateFaceInfo);
                
            }

            if (trackingStatus.Candidates.Count > 1)
            {
                var bestOfTheRest = GetBestOfTheRest(trackingStatus, t);
                if (bestOfTheRest.similarity > MatchingParameters.InstantMatchThreshold)
                {
                    var match = Matched(bestOfTheRest.candidate, bestOfTheRest.similarity, bestOfTheRest.snapshot, _faceDb[bestOfTheRest.candidate.FaceId]);
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

            if (topCandidateMatch.similarity > MatchingParameters.NewTemplateThreshold)
            {
                AddTemplate(t, topCandidate);
            }

            if (topCandidateMatch.similarity > MatchingParameters.MatchThreshold)
            {
                return Matched(topCandidate, topCandidateMatch.similarity, topCandidateMatch.snapshot, _faceDb[topCandidate.FaceId]);
            }

            return ProcessUntracked(t);
        }

        private Match<byte[]> ProcessUntracked(FaceTemplate t)
        {
            var bestMatch = _faceDb.GetBestMatch(t);
            if (bestMatch == null) return new Match<byte[]>{ IsValid = false };

            if (bestMatch.Similarity <= MatchingParameters.MatchThreshold)
            {
                bestMatch.IsValid = false;
                return bestMatch;
            }

            _trackedFaces[t.TrackingId] =
                new TrackingStatus(new CandidateStatus { Confirmations = bestMatch.Similarity, FaceId = bestMatch.FaceId });

            return bestMatch;
        }

        private TrackingStatus CaptureImpl(FaceTemplate template, bool forceNewFace)
        {
            if (forceNewFace || !_trackedFaces.TryGetValue(template.TrackingId, out var trackingStatus))
            {
                trackingStatus = _trackedFaces[template.TrackingId] = new TrackingStatus(new CandidateStatus { FaceId = _faceDb.NextId, Confirmations = 1 });
            }

            AddTemplate(template, trackingStatus.TopCandidate);

            return trackingStatus;
        }

        private Match<byte[]> Matched(CandidateStatus cs, float confidence, FaceSnapshot<byte[]> snapshot, IFaceInfo<byte[]> faceInfo)
        {
            cs.Confirmations += confidence;
            return new Match<byte[]>(cs.FaceId, confidence, snapshot, faceInfo);
        }

        private void AddTemplate(FaceTemplate template, CandidateStatus candidateStatus)
        {
            _faceDb.AddOrUpdate(candidateStatus.FaceId, template);
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
        public IEnumerable<KeyValuePair<long, Match<byte[]>>> ProcessTemplates(IEnumerable<FaceTemplate> templates)
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
                .Select(t =>
                {
                    var match = ProcessTemplate(t);
                    return new KeyValuePair<long, Match<byte[]>>(t.TrackingId, match);
                })
                .Where(pair => pair.Value.IsValid && pair.Value.Similarity >= MatchingParameters.MatchThreshold);
        }
    }

    public class MatchingParameters
    {
        public float InstantMatchThreshold { get; set; }
        public float NewTemplateThreshold { get; set; }
        public float MatchThreshold { get; set; }

        public static MatchingParameters Default => new MatchingParameters
        {
            InstantMatchThreshold = FsdkSettings.Default.InstantMatchThreshold,
            NewTemplateThreshold = FsdkSettings.Default.NewTemplateThreshold,
            MatchThreshold = FsdkSettings.Default.MatchThreshold,
        };
    }
}