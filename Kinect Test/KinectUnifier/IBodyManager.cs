using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectUnifier
{
    public interface IBodyManager
    {
        event EventHandler<BodyFrameReadyEventArgs> BodyFrameReady;
        int BodyCount { get; }
    }

    public class BodyFrameReadyEventArgs
    {
        private IBodyFrame _bodyFrame;

        public BodyFrameReadyEventArgs(IBodyFrame bodyFrame)
        {
            _bodyFrame = bodyFrame;
        }
    }

    public interface IBodyFrame : IDisposable
    {
        void CopyBodiesTo(IBody[] bodies);
    }

    public interface IJoint
    {
        Point3F CameraSpacePoint { get; }
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
