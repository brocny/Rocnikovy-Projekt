using System;
using Core;
using Core.Kinect;
using Microsoft.Kinect;

namespace Kinect360
{
    public class MultiManager360 : IMultiFrameStream
    {
        public MultiManager360(Kinect360 kinect, MultiFrameTypes frameTypes, bool preferResolutionOverFps)
        {
            _kinect360 = kinect;
            var colorImageFormat = preferResolutionOverFps
                ? ColorImageFormat.RgbResolution1280x960Fps12
                : ColorImageFormat.RgbResolution640x480Fps30;
            (_kinect360.ColorFrameStream as ColorFrameStream360).ColorImageFormat = ColorImageFormat.RgbResolution1280x960Fps12;
            _kinect360.KinectSensor.ColorStream.Enable(colorImageFormat);
            _kinect360.KinectSensor.SkeletonStream.Enable();
            _kinect360.KinectSensor.DepthStream.Enable();
            _kinect360.Open();
            _kinect360.KinectSensor.AllFramesReady += KinectSensor_AllFramesReady;
            FrameTypes = frameTypes;
        }

        private void KinectSensor_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            DepthFrameStream360.DepthFrame depthFrame360 = null;
            BodyFrameStream360.BodyFrame bodyFrame360 = null;
            ColorFrameStream360.ColorFrame colorFrame360 = null;

            if (FrameTypes.HasFlag(MultiFrameTypes.Color))
            {
                var colorFrame = e.OpenColorImageFrame();
                if(colorFrame == null)
                    return;

                colorFrame360 = new ColorFrameStream360.ColorFrame(colorFrame);
            }

            if (FrameTypes.HasFlag(MultiFrameTypes.Body))
            {
                var skeletonFrame = e.OpenSkeletonFrame();
                if (skeletonFrame == null)
                    return;
               
                bodyFrame360 = new BodyFrameStream360.BodyFrame(skeletonFrame);
            }

            if (FrameTypes.HasFlag(MultiFrameTypes.Depth))
            {
                var depthFrame = e.OpenDepthImageFrame();
                if(depthFrame == null)
                    return;

                depthFrame360 = new DepthFrameStream360.DepthFrame(depthFrame);
            }
            
            MultiFrameArrived?.Invoke(this, new MultiFrameReadyEventArgs(new MultiFrame(colorFrame360, bodyFrame360, depthFrame360)));
        }

        

        public event EventHandler<MultiFrameReadyEventArgs> MultiFrameArrived;
        public MultiFrameTypes FrameTypes { get; }

        private readonly Kinect360 _kinect360;

        public void Dispose()
        {
            _kinect360.KinectSensor.AllFramesReady -= KinectSensor_AllFramesReady;
        }

        public class MultiFrame : IMultiFrame
        {
            public MultiFrame(IColorFrame colorFrame, IBodyFrame bodyFrame, IDepthFrame depthFrame)
            {
                ColorFrame = colorFrame;
                BodyFrame = bodyFrame;
                DepthFrame = depthFrame;
            }

            public IColorFrame ColorFrame { get; }
            public IBodyFrame BodyFrame { get; }
            public IDepthFrame DepthFrame { get; }

            public void Dispose()
            {
                ColorFrame?.Dispose();
                BodyFrame?.Dispose();
                DepthFrame?.Dispose();
            }
        }
    }
}
