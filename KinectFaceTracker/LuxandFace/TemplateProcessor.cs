using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Face;
using KinectUnifier;
using LuxandFaceLib;

namespace LuxandFace
{
    class TemplateProcessor
    {
        public TemplateProcessor(IFaceDatabase<byte[]> faceDb, IReadOnlyDictionary<long, TrackingStatus> trackedFaces = null)
        {
            _faceDb = faceDb;
            _trackedFaces = trackedFaces == null
                ? new ConcurrentDictionary<long, TrackingStatus>()
                : new ConcurrentDictionary<long, TrackingStatus>(trackedFaces);
        }


        public IReadOnlyDictionary<long, TrackingStatus> TrackedFaces => _trackedFaces;

        public void AddTemplate(long trackingId)
        {
            _addTemplates.Add(trackingId);
        }

        public MatchingParameters MatchingParameters
        {
            get => _mp;
            set => _mp = value;
        }
        private MatchingParameters _mp = MatchingParameters.Default;

        public Match ProcessTemplate(FaceTemplate t)
        {
            if (_addTemplates.Remove(t.TrackingId))
            {
                Capture(t);
                return null;
            }

            if (_trackedFaces.TryGetValue(t.TrackingId, out var trackingStatus))
            {
                trackingStatus.WasSeen = true;
                var topCandidate = trackingStatus.TopCandidate;
                var topCondidateFaceInfo = _faceDb[topCandidate.FaceId];
                float topCandidateSim = topCondidateFaceInfo.GetSimilarity(t);

                if (topCandidateSim > _mp.TrackedInstantMatchThreshold)
                {
                    return Matched(t, topCandidate, topCandidateSim);
                }

                if (trackingStatus.Candidates.Count > 1)
                {
                    var bestOfTheRest = GetBestOfTheRest(trackingStatus, t);
                    if (bestOfTheRest.confidence > _mp.TrackedInstantMatchThreshold)
                    {
                        var match = Matched(t, bestOfTheRest.status, bestOfTheRest.confidence);
                        if (bestOfTheRest.status.Confirmations > topCandidate.Confirmations)
                        {
                            trackingStatus.TopCandidate = bestOfTheRest.status;
                        }
                        return match;
                    }

                    if (bestOfTheRest.confidence > topCandidateSim)
                    {
                        topCandidate = bestOfTheRest.status;
                        topCandidateSim = bestOfTheRest.confidence;
                    }
                }

                if (topCandidateSim > _mp.TrackedNewTemplateThreshold)
                {
                    NewTemplate(t, topCandidate);
                    return Matched(t, topCandidate, topCandidateSim);
                }

                if (topCandidateSim > _mp.MatchThreshold)
                {
                    return Matched(t, topCandidate, topCandidateSim);
                }
            }
            
            return ProcessUntracked(t);
        }

        private Match ProcessUntracked(FaceTemplate t)
        {
            
            var bestMatch = _faceDb.GetBestMatch(t);
            if (bestMatch.confidence <= _mp.MatchThreshold)
            {
                return null;
            }

            _trackedFaces[t.TrackingId] = new TrackingStatus(new CandidateStatus { Confirmations = bestMatch.confidence, FaceId = bestMatch.id });

            if (bestMatch.confidence > _mp.UntrackedNewTemplateThreshold &&
                bestMatch.confidence < _mp.UntrackedInstantMatchThreshold)
            {
                _faceDb[bestMatch.id].AddTemplate(t);
            }

            return new Match {Confidence =  bestMatch.confidence, FaceId = bestMatch.id, TrackingId = t.TrackingId};
        }

        public void Capture(FaceTemplate t)
        {
            if (!_trackedFaces.TryGetValue(t.TrackingId, out var ts))
            {
                ts = _trackedFaces[t.TrackingId] = new TrackingStatus(new CandidateStatus{FaceId = _faceDb.NextId});
            }

            NewTemplate(t, ts.TopCandidate);

        }

        private Match Matched(FaceTemplate t, CandidateStatus cs, float confidence)
        {
            cs.Confirmations += confidence;
            return new Match {Confidence = confidence, FaceId = cs.FaceId, TrackingId = t.TrackingId};
        }

        private void NewTemplate(FaceTemplate t, CandidateStatus ts)
        {
            _faceDb.AddOrUpdate(ts.FaceId, t);
        }

        /// <summary>
        /// Gets the best-matching candidate in <paramref name="ts"/> excluding the <see cref="TrackingStatus.TopCandidate"/>
        /// </summary>
        /// <param name="ts">Where to look for candidates</param>
        /// <param name="template">What to match candidates to</param>
        /// <returns>The best-maching <see cref="CandidateStatus"/> and match confidence (between 0 and 1).</returns>
        private (CandidateStatus status, float confidence) GetBestOfTheRest(TrackingStatus ts, IFaceTemplate<byte[]> template)
        {
            float maxConfidence = 0f;
            CandidateStatus status = null;
            var candidates = ts.Candidates;
            for (int i = 1; i < candidates.Count; i++)
            {

                var cand = candidates[i];
                float conf = _faceDb[cand.FaceId].GetSimilarity(template);
                if (conf > maxConfidence)
                {
                    maxConfidence = conf;
                    status = cand;
                }
            }

            return (status, maxConfidence);
        }

        /// <summary>
        /// Process multiple templates at once
        /// </summary>
        /// <param name="templates">Templates to be processed</param>
        /// <returns>Any matches that might be found</returns>
        public IEnumerable<Match> ProcessTemplates(IEnumerable<FaceTemplate> templates)
        {
            var list = new List<Match>();

            foreach (var template in templates)
            {
                var match = ProcessTemplate(template);
                if (match != null && match.Confidence > _mp.MatchThreshold)
                {
                    list.Add(match);
                }
            }

            if (_cacheClearingCounter++ > ClearCacheIterationsLimit)
            {
                foreach (var ts in _trackedFaces.Where(t => !t.Value.WasSeen).ToList())
                {
                    _trackedFaces.TryRemove(ts.Key, out _);
                }

                _cacheClearingCounter = 0;
            }

            return list;
        }

        private ushort _cacheClearingCounter = ClearCacheIterationsLimit;
        private const ushort ClearCacheIterationsLimit = 0;
        private ConcurrentDictionary<long, TrackingStatus> _trackedFaces;
        private IFaceDatabase<byte[]> _faceDb;

        private ConcurrentHashSet<long> _addTemplates = new ConcurrentHashSet<long>();
    }

    public class Match
    {
        public long TrackingId;
        public int FaceId;
        public float Confidence;
    }

    public class MatchingParameters
    {
        public float TrackedInstantMatchThreshold { get; set; } 
        public float UntrackedInstantMatchThreshold { get; set; }
        public float TrackedNewTemplateThreshold { get; set; }
        public float MatchThreshold { get; set; }
        public float UntrackedNewTemplateThreshold { get; set; }

        public static MatchingParameters Default => new MatchingParameters
        {
            TrackedInstantMatchThreshold = DefaultTrackedInstantMatchThreshold,
            UntrackedInstantMatchThreshold = DefaultUntrackedInstantMatchThreshold,
            TrackedNewTemplateThreshold = DefaultTrackedNewTemplateThreshold,
            MatchThreshold = DefaultMatchThreshold,
            UntrackedNewTemplateThreshold = DefaultUntrackedNewTemplateThreshold
        };

        private const float
            DefaultTrackedInstantMatchThreshold = 0.92f,
            DefaultUntrackedInstantMatchThreshold = 0.75f,
            DefaultTrackedNewTemplateThreshold = 0.4f,
            DefaultMatchThreshold = 0.5f,
            DefaultUntrackedNewTemplateThreshold = 0.6f;
    }
}
