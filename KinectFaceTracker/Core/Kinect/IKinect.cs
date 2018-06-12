using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Core.Kinect;

namespace Core.Kinect
{
    public interface IKinect
    {
        void Open();
        void Close();

        IBodyFrameStream BodyFrameStream { get; }
        IColorFrameStream ColorFrameStream { get; }
        IDepthFrameStream DepthFrameStream { get; }
        ICoordinateMapper CoordinateMapper { get; }
        IMultiFrameStream OpenMultiManager(MultiFrameTypes frameTypes, bool preferResolutionOverFps = false);


        bool IsRunning { get; }
    }
}
