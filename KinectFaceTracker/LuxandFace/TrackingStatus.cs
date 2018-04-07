using System.Collections.Generic;

namespace LuxandFace
{
    public class TrackingStatus
    {
        public TrackingStatus(CandidateStatus topCandidate)
        {
            _candidates = new List<CandidateStatus>{ topCandidate };
        }

        internal TrackingStatus()
        {
            _candidates = new List<CandidateStatus>();
        }

        public IList<CandidateStatus> Candidates => _candidates;

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

        private List<CandidateStatus> _candidates;
        public uint UnseenCounter { get; set; }
    }

    public class CandidateStatus
    {
        public float Confirmations;
        public uint SkippedFrames;
        public int FaceId;
    }
}
