using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace KinectUnifier
{
    public interface IBodyManager
    {
        event EventHandler<BodyFrameReadyEventArgs> BodyFrameReady;
        void Open();
        void Close();
        int BodyCount { get; }
    }

    public class BodyFrameReadyEventArgs
    {
        public IBodyFrame BodyFrame;

        public BodyFrameReadyEventArgs(IBodyFrame bodyFrame)
        {
            BodyFrame = bodyFrame;
        }
    }

    public interface IBodyFrame : IDisposable
    {
        void CopyBodiesTo(IBody[] bodies);

        int BodyCount { get; }
        
        Point4F FloorClipPlane { get; }
    }

    public interface IJoint
    {
        Point3F Position { get; }
        bool IsTracked { get; }
    }

    public enum JointType
    {
        HipCenter, SpineMid, Neck, Head,
        ShoulderLeft, ElbowLeft, WristLeft, HandLeft,
        ShoulderRight, ElbowRight, WristRight, HandRight,
        HipLeft, KneeLeft, AnkleLeft, FootLeft,
        HipRight, KneeRight, AnkleRight, FootRight,
        ShoulderCenter
    }


    public interface IBody
    {
        IReadOnlyDictionary<JointType, IJoint> Joints { get; }
        bool IsTracked { get; }
    }

}
