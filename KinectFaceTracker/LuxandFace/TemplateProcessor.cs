using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Face;
using LuxandFaceLib;

namespace LuxandFace
{
    class TemplateProcessor
    {
        public TemplateProcessor(FaceDatabase<byte[]> faceDb, IReadOnlyDictionary<long, TrackingStatus> trackedFaces = null)
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
            if (_addTemplates.Contains(t.TrackingId))
            {
                _addTemplates.Remove(t.TrackingId);
                Capture(t);
                return null;
            }

            if (_trackedFaces.TryGetValue(t.TrackingId, out var trackingStatus))
            {
                var topCandidate = trackingStatus.Candidates[0];
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

        private (CandidateStatus status, float confidence) GetBestOfTheRest(TrackingStatus ts, FaceTemplate template)
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

            return list;
        }


        private ConcurrentDictionary<long, TrackingStatus> _trackedFaces;
        private FaceDatabase<byte[]> _faceDb;

        private HashSet<long> _addTemplates = new HashSet<long>();
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
