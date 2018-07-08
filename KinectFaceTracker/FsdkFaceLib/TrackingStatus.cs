using System.Collections.Generic;

namespace FsdkFaceLib
{
    public class TrackingStatus
    {
        public TrackingStatus(TrackedCandidate topTrackedCandidate)
        {
            _candidates = new List<TrackedCandidate> { topTrackedCandidate };
        }

        public IList<TrackedCandidate> Candidates => _candidates;
        public bool WasSeen { get; set; } = true;
        public int SkippedFrames { get; set; }

        public TrackedCandidate TopTrackedCandidate
        {
            get => _candidates[0];
            set
            {
                lock (_candidates)
                {
                    var temp = _candidates[0];
                    _candidates[0] = value;
                    _candidates.Add(temp);
                }
            }
        }

        private readonly List<TrackedCandidate> _candidates;
    }

    public class TrackedCandidate
    {
        public TrackedCandidate()
        {

        }

        public TrackedCandidate(float fusionScore, int faceId)
        {
            FusionScore = fusionScore;
            FaceId = faceId;
        }

        public float FusionScore { get; set; }
        public int FaceId { get; set; }
    }
}
