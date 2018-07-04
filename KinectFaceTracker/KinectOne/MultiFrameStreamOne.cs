using System;
using Core.Kinect;
using Microsoft.Kinect;

namespace KinectOne
{
    public class MultiFrameStreamOne : IMultiFrameStream
    {
        public MultiFrameStreamOne(MultiFrameTypes frameTypes, KinectOne kinect)
        {
            FrameTypes = frameTypes;
            _kinectOne = kinect;
            _multiReader = _kinectOne.KinectSensor.OpenMultiSourceFrameReader((FrameSourceTypes)(int) frameTypes);
            _multiReader.MultiSourceFrameArrived += MultiReaderOnMultiSourceFrameArrived;
        }

        public event EventHandler<MultiFrameReadyEventArgs> MultiFrameArrived;
        public MultiFrameTypes FrameTypes { get; }

        private void MultiReaderOnMultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            var multiFrame = e.FrameReference.AcquireFrame();
            if (multiFrame != null)
            {
                var colorFrame = multiFrame.ColorFrameReference.AcquireFrame();
                var bodyFrame = multiFrame.BodyFrameReference.AcquireFrame();
                var depthFrame = multiFrame.DepthFrameReference.AcquireFrame();
                var colorFrameOne = colorFrame == null ? null : new ColorFrameStreamOne.ColorFrame(colorFrame);
                var bodyFrameOne = bodyFrame == null ? null : new BodyFrameStreamOne.BodyFrame(bodyFrame);
                var depthFrameOne = depthFrame == null ? null : new DepthFrameStreamOne.DepthFrame(depthFrame);
                MultiFrameArrived?.Invoke(this, new MultiFrameReadyEventArgs(new MultiFrame(colorFrameOne, bodyFrameOne, depthFrameOne)));
            }
        }

        private readonly KinectOne _kinectOne;
        private readonly MultiSourceFrameReader _multiReader;

        public void Dispose()
        {
            _multiReader.MultiSourceFrameArrived -= MultiReaderOnMultiSourceFrameArrived;
            _multiReader?.Dispose();
        }

        public class MultiFrame : IMultiFrame
        {
            public MultiFrame(ColorFrameStreamOne.ColorFrame colorFrame, BodyFrameStreamOne.BodyFrame bodyFrame, DepthFrameStreamOne.DepthFrame depthFrame)
            {
                _bodyFrame = bodyFrame;
                _colorFrame = colorFrame;
                _depthFrame = depthFrame;
            }

            private readonly ColorFrameStreamOne.ColorFrame _colorFrame;
            private readonly BodyFrameStreamOne.BodyFrame _bodyFrame;
            private readonly DepthFrameStreamOne.DepthFrame _depthFrame;

            public IColorFrame ColorFrame => _colorFrame;
            public IBodyFrame BodyFrame => _bodyFrame;
            public IDepthFrame DepthFrame => _depthFrame;

            public void Dispose()
            {
                _colorFrame?.Dispose();
                _bodyFrame?.Dispose();
                _depthFrame?.Dispose();
            }
        }
    }
}
