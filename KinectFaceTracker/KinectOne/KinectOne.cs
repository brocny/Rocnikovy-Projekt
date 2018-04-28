using Core;
using Microsoft.Kinect;

namespace KinectOne
{
    public class KinectOne : IKinect
    {
        public KinectSensor KinectSensor { get; private set; }

        public bool IsKinectOne => true;
        public bool IsAvailable => KinectSensor.IsAvailable;

        public IBodyManager BodyManager => _bodyManager ?? (_bodyManager = new BodyManagerOne(this));
        public IColorManager ColorManager => _colorManager ?? (_colorManager = new ColorManagerOne(this));
        public ICoordinateMapper CoordinateMapper => _coordinateMapper ?? (_coordinateMapper = new CoordinateMapperOne(KinectSensor.CoordinateMapper));
        
        public IMultiManager OpenMultiManager(MultiFrameTypes frameTypes, bool preferResolutionOverFps = false)
        {
            return new MultiFrameManagerOne(frameTypes, this);
        }

        private BodyManagerOne _bodyManager;
        private ColorManagerOne _colorManager;
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
