﻿using System;
using System.Linq;
using Core.Kinect;
using Microsoft.Kinect;

namespace Kinect360
{
    public class Kinect360 : IKinect
    {
        public KinectSensor KinectSensor { get; }

        public IBodyFrameStream BodyFrameStream =>  _bodyFrameStream ?? (_bodyFrameStream = new BodyFrameStream360(this));
        public IColorFrameStream ColorFrameStream => _colorFrameStream ?? (_colorFrameStream = new ColorFrameStream360(this));
        public IDepthFrameStream DepthFrameStream => _depthFrameStream ?? (_depthFrameStream = new DepthFrameStream360(this));
        public ICoordinateMapper CoordinateMapper => _coordinateMapper ?? (_coordinateMapper = new CoordinateMapper360(this));
        public IMultiFrameStream OpenMultiManager(MultiFrameTypes frameTypes, bool preferResolutionOverFps = true)
        {
            return new MultiManager360(this, frameTypes, preferResolutionOverFps);
        }

        public bool IsRunning => KinectSensor.IsRunning;
        
        private BodyFrameStream360 _bodyFrameStream;
        private DepthFrameStream360 _depthFrameStream;
        internal ColorFrameStream360 _colorFrameStream;
        private CoordinateMapper360 _coordinateMapper;

        private ColorImageFormat _colorImageFormat;

        public Kinect360()
        {
            KinectSensor = KinectSensor.KinectSensors
                .FirstOrDefault(kinectSensor => kinectSensor.Status == KinectStatus.Connected);

            if (KinectSensor == null)
                throw new ApplicationException("No Kinect device found!");
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