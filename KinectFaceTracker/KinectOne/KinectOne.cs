using Core;
using Core.Kinect;
using Microsoft.Kinect;

namespace KinectOne
{
    public class KinectOne : IKinect
    {
        public KinectSensor KinectSensor { get; private set; }

        public bool IsKinectOne => true;
        public bool IsAvailable => KinectSensor.IsAvailable;

        public IBodyFrameStream BodyFrameStream => _bodyFrameStream ?? (_bodyFrameStream = new BodyFrameStreamOne(this));
        public IColorFrameStream ColorFrameStream => _colorFrameStream ?? (_colorFrameStream = new ColorFrameStreamOne(this));
        public IDepthFrameStream DepthFrameStream => _depthSource ?? (_depthSource = new DepthFrameStreamOne(this));
        public ICoordinateMapper CoordinateMapper => _coordinateMapper ?? (_coordinateMapper = new CoordinateMapperOne(KinectSensor.CoordinateMapper));
        
        public IMultiFrameStream OpenMultiManager(MultiFrameTypes frameTypes, bool preferResolutionOverFps = false)
        {
            return new MultiFrameStreamOne(frameTypes, this);
        }

        private BodyFrameStreamOne _bodyFrameStream;
        private ColorFrameStreamOne _colorFrameStream;
        private DepthFrameStreamOne _depthSource;
        private CoordinateMapperOne _coordinateMapper;
        
        public bool IsRunning => KinectSensor.IsOpen;

        public KinectOne()
        {
            KinectSensor = KinectSensor.GetDefault();
        }

        public void Open()
        {
            KinectSensor.Open();
        }

        public void Close()
        {
            KinectSensor.Close();
        }

    }
}
