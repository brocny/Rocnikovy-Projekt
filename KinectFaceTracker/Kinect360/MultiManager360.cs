using System;
using Core;

namespace Kinect360
{
    public class MultiManager360 : IMultiManager
    {
        public MultiManager360(Kinect360 kinect, MultiFrameTypes frameTypes, bool preferResolutionOverFps)
        {
            _kinect360 = kinect;
            FrameTypes = frameTypes;
            if (frameTypes.HasFlag(MultiFrameTypes.Body))
            {
                _kinect360.BodyManager.Open();
                _kinect360.BodyManager.BodyFrameReady += BodyManagerOnBodyFrameReady;          
            }

            if (frameTypes.HasFlag(MultiFrameTypes.Color))
            {
                _kinect360.ColorManager.Open(preferResolutionOverFps);
                _kinect360.ColorManager.ColorFrameReady += ColorManagerOnColorFrameReady;
            }
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

        private Kinect360 _kinect360;

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
