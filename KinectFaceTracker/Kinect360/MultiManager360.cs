using System;
using Core;
using Microsoft.Kinect;

namespace Kinect360
{
    public class MultiManager360 : IMultiManager
    {
        public MultiManager360(Kinect360 kinect, MultiFrameTypes frameTypes, bool preferResolutionOverFps)
        {
            _kinect360 = kinect;
            var colorImageFormat = preferResolutionOverFps
                ? ColorImageFormat.RgbResolution1280x960Fps12
                : ColorImageFormat.RgbResolution640x480Fps30;
            (_kinect360.ColorManager as ColorManager360).ColorImageFormat = ColorImageFormat.RgbResolution1280x960Fps12;
            _kinect360.KinectSensor.ColorStream.Enable(colorImageFormat);
            _kinect360.KinectSensor.SkeletonStream.Enable();
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

                _colorFrame = new ColorManager360.ColorFrame360(colorFrame);
            }

            if (FrameTypes.HasFlag(MultiFrameTypes.Body))
            {
                var skeletonFrame = e.OpenSkeletonFrame();
                if (skeletonFrame == null)
                    return;
               
                _bodyFrame = new BodyManager360.BodyFrame360(skeletonFrame);
            }
            
            MultiFrameArrived?.Invoke(this, new MultiFrameReadyEventArgs(new MultiFrame360(_colorFrame, _bodyFrame)));
        }

        private void ColorManagerOnColorFrameReady(object sender, ColorFrameReadyEventArgs e)
        {
            var cFrame = e.ColorFrame;
            if (cFrame != null)
            {
                _colorFrame?.Dispose();
                _colorFrame = cFrame;
            }

            if(!_hasNewBodyFrame && FrameTypes.HasFlag(MultiFrameTypes.Body))
            {
                _hasNewColorFrame = true;
                return;
            }

            _hasNewBodyFrame = false;
            _hasNewColorFrame = false;
            MultiFrameArrived?.Invoke(this, new MultiFrameReadyEventArgs(new MultiFrame360(_colorFrame, _bodyFrame)));
        }

        private void BodyManagerOnBodyFrameReady(object sender, BodyFrameReadyEventArgs e)
        {
            var bFrame = e.BodyFrame;
            if (bFrame != null)
            {
                _bodyFrame?.Dispose();
                _bodyFrame = bFrame;
            }

            if (!_hasNewColorFrame && FrameTypes.HasFlag(MultiFrameTypes.Color))
            {
                _hasNewBodyFrame = true;
                return;
            }

            _hasNewBodyFrame = false;
            _hasNewColorFrame = false;
            MultiFrameArrived?.Invoke(this, new MultiFrameReadyEventArgs(new MultiFrame360(_colorFrame, _bodyFrame)));
        }

        public event EventHandler<MultiFrameReadyEventArgs> MultiFrameArrived;
        public MultiFrameTypes FrameTypes { get; }
        private bool _hasNewColorFrame;
        private bool _hasNewBodyFrame;

        private IColorFrame _colorFrame;
        private IBodyFrame _bodyFrame;

        private readonly Kinect360 _kinect360;

        public void Dispose()
        {
            _colorFrame?.Dispose();
            _bodyFrame?.Dispose();
            if (FrameTypes.HasFlag(MultiFrameTypes.Color))
            {
                _kinect360.ColorManager.ColorFrameReady -= ColorManagerOnColorFrameReady;
            }

            if (FrameTypes.HasFlag(MultiFrameTypes.Body))
            {
                _kinect360.BodyManager.BodyFrameReady -= BodyManagerOnBodyFrameReady;
            }
        }
    }

    public class MultiFrame360 : IMultiFrame
    {
        public MultiFrame360(IColorFrame colorFrame, IBodyFrame bodyFrame)
        {
            ColorFrame = colorFrame;
            BodyFrame = bodyFrame;
        }

        public IColorFrame ColorFrame { get; }
        public IBodyFrame BodyFrame { get; }

        public void Dispose()
        {
            ColorFrame?.Dispose();
            BodyFrame?.Dispose();
        }
    }
}
