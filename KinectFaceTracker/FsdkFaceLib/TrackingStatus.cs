using System.Collections.Generic;

namespace FsdkFaceLib
{
    public class TrackingStatus
    {
        public TrackingStatus(CandidateStatus topCandidate)
        {
            _candidates = new List<CandidateStatus> { topCandidate };
        }

        public IList<CandidateStatus> Candidates => _candidates;
        public bool WasSeen { get; set; } = true;
        public int SkippedFrames { get; set; }

        public CandidateStatus TopCandidate
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

        private readonly List<CandidateStatus> _candidates;
    }

    public class CandidateStatus
    {
        public CandidateStatus()
        {

        }

        public CandidateStatus(float confirmations, int faceId)
        {
            Confirmations = confirmations;
            FaceId = faceId;
        }

        public float Confirmations { get; set; }
        public int FaceId { get; set; }
    }
}
