using KinectUnifier;
using Microsoft.Kinect;

namespace KinectOne
{
    public class KinectOne : IKinect
    {
        public KinectSensor KinectSensor { get; private set; }

        public bool IsKinectOne => true;

        public IBodyManager BodyManager => _bodyManager;
        public IColorManager ColorManager => _colorManager;
        public ICoordinateMapper CoordinateMapper => _coordinateMapper ?? (_coordinateMapper = new CoordinateMapperOne(KinectSensor.CoordinateMapper));

        private BodyManagerOne _bodyManager;
        private ColorManagerOne _colorManager;
        private CoordinateMapperOne _coordinateMapper;
        
        public bool IsRunning => KinectSensor.IsOpen;

        public KinectOne()
        {
            KinectSensor = KinectSensor.GetDefault();
            _bodyManager = new BodyManagerOne(this);
            _colorManager = new ColorManagerOne(this);
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
