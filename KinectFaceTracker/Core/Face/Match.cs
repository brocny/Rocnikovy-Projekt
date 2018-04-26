using System;

namespace Face
{
    public class Match<T> : IComparable<Match<T>>
    {
        public Match(int faceId, float similarity, FaceSnapshot<T> snapshot, IFaceInfo<T> faceInfo, long trackingId = 0)
        {
            FaceId = faceId;
            Similarity = similarity;
            Snapshot = snapshot;
            FaceInfo = faceInfo;
            TrackingId = trackingId;
        }

        public int FaceId { get; }
        public float Similarity { get; }
        public FaceSnapshot<T> Snapshot { get; }
        public IFaceInfo<T> FaceInfo { get; }
        public long TrackingId { get; set; }

        public int CompareTo(Match<T> other)
        {
            return Similarity.CompareTo(other.Similarity);
        }
    }
}