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
            if (FrameTypes.HasFlag(MultiFrameTypes.Color))
            {
                var colorFrame = e.OpenColorImageFrame();
                if(colorFrame == null)
                    return;

                _colorFrame = new ColorFrameStream360.ColorFrame360(colorFrame);
            }

            if (FrameTypes.HasFlag(MultiFrameTypes.Body))
            {
                var skeletonFrame = e.OpenSkeletonFrame();
                if (skeletonFrame == null)
                    return;
               
                _bodyFrame = new BodyFrameStream360.BodyFrame360(skeletonFrame);
            }

            if (FrameTypes.HasFlag(MultiFrameTypes.Depth))
            {
                var depthFrame = e.OpenDepthImageFrame();
                if(depthFrame == null)
                    return;

                _depthFrame = new DepthFrameStream360.DepthFrame360(depthFrame);
            }
            
            MultiFrameArrived?.Invoke(this, new MultiFrameReadyEventArgs(new MultiFrame360(_colorFrame, _bodyFrame, _depthFrame)));
        }

        

        public event EventHandler<MultiFrameReadyEventArgs> MultiFrameArrived;
        public MultiFrameTypes FrameTypes { get; }

        private IColorFrame _colorFrame;
        private IBodyFrame _bodyFrame;
        private IDepthFrame _depthFrame;

        private readonly Kinect360 _kinect360;

        public void Dispose()
        {
            _colorFrame?.Dispose();
            _bodyFrame?.Dispose();
            _depthFrame?.Dispose();
            _kinect360.KinectSensor.AllFramesReady -= KinectSensor_AllFramesReady;
        }
    }

    public class MultiFrame360 : IMultiFrame
    {
        public MultiFrame360(IColorFrame colorFrame, IBodyFrame bodyFrame, IDepthFrame depthFrame)
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
