using System.Collections.Generic;

namespace FsdkFaceLib
{
    public class TrackingStatus
    {
        public TrackingStatus(CandidateStatus topCandidate)
        {
            _candidates = new List<CandidateStatus> {topCandidate};
        }

        public IList<CandidateStatus> Candidates => _candidates;
        public bool WasSeen { get; set; } = true;
        public int SkippedFrames { get; set; }

        public CandidateStatus TopCandidate
        {
            get => _candidates[0];
            set
            {
                var temp = _candidates[0];
                _candidates[0] = value;
                _candidates.Add(temp);
            }
        }

        private readonly List<CandidateStatus> _candidates;
    }

    public class CandidateStatus
    {
        public float Confirmations;
        public int FaceId;
    }
}
