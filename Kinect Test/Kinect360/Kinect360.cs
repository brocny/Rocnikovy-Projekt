using System;
using System.Linq;
using KinectUnifier;
using Microsoft.Kinect;


namespace Kinect360
{
    public class Kinect360 : IKinect
    {
        public KinectSensor KinectSensor { get; private set; }
        public bool IsKinectOne => false;

        public IBodyManager BodyManager => _bodyManager;
        public IColorManager ColorManager => _colorManager;
        public ICoordinateMapper CoordinateMapper => _coordinateMapper ?? (_coordinateMapper = new CoordinateMapper360(this));

        public bool IsRunning => KinectSensor.IsRunning;
        
        private BodyManager360 _bodyManager;
        internal ColorManager360 _colorManager;
        private CoordinateMapper360 _coordinateMapper;

        private ColorImageFormat _colorImageFormat;

        public Kinect360()
        {
            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    KinectSensor = potentialSensor;
                    break;
                }
            }
            if (KinectSensor == null)
                throw new Exception("No Kinect device found!");
            
            _bodyManager = new BodyManager360(this);
            _colorManager = new ColorManager360(this);
        }

        public void Open()
        {
            KinectSensor.Start();
        }

        public void Close()
        {
            KinectSensor.Stop();
        }
    }


}