using System;
using System.Collections.Generic;
using System.Numerics;

namespace KinectUnifier
{
    public interface IBodyManager
    {
        event EventHandler<BodyFrameReadyEventArgs> BodyFrameReady;
        void Open();
        void Close();
        IBodyFrame GetNextFrame();
        int BodyCount { get; }
    }

    public class BodyFrameReadyEventArgs
    {
        public IBodyFrame BodyFrame { get; }

        public BodyFrameReadyEventArgs(IBodyFrame bodyFrame)
        {
            BodyFrame = bodyFrame;
        }
    }

    public interface IBodyFrame : IDisposable
    {
        void CopyBodiesTo(IBody[] bodies);

        int BodyCount { get; }
        
        Vector4 FloorClipPlane { get; }
    }

    public interface IJoint
    {
        Vector3 Position { get; }
        bool IsTracked { get; }
        JointType JointType { get; }
    }

    public enum JointType
    {
        HipCenter, SpineMid, Neck, Head,
        ShoulderLeft, ElbowLeft, WristLeft, HandLeft,
        ShoulderRight, ElbowRight, WristRight, HandRight,
        HipLeft, KneeLeft, AnkleLeft, FootLeft,
        HipRight, KneeRight, AnkleRight, FootRight,
        ShoulderCenter,
        HandTipLeft, ThumbLeft,
        HandTipRight, ThumbRight
    }


    public interface IBody
    {
        IReadOnlyDictionary<JointType, IJoint> Joints { get; }
        IReadOnlyList<(JointType joint1, JointType joint2)> Bones { get; }
        bool IsTracked { get; }
        long TrackingId { get; }
    }

}
