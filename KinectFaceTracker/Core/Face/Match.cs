﻿using System;

namespace Core.Face
{
    public class Match<TTemplate> : IComparable<Match<TTemplate>>
    {
        public Match() { }

        public Match(int faceId, float similarity, FaceSnapshot<TTemplate> snapshot, IFaceInfo<TTemplate> faceInfo, long trackingId = 0, bool isValid = true)
        {
            FaceId = faceId;
            Similarity = similarity;
            Snapshot = snapshot;
            FaceInfo = faceInfo;
            TrackingId = trackingId;
            IsValid = isValid;
        }

        public int FaceId { get; }
        public float Similarity { get; }
        public FaceSnapshot<TTemplate> Snapshot { get; }
        public IFaceInfo<TTemplate> FaceInfo { get; }
        public long TrackingId { get; set; }
        public bool IsValid { get; set; }

        public int CompareTo(Match<TTemplate> other)
        {
            return Similarity.CompareTo(other.Similarity);
        }
    }
}