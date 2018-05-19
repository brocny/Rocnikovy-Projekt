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
                var colorFrameOne = colorFrame == null ? null : new ColorFrameStreamOne.ColorFrameOne(colorFrame);
                var bodyFrameOne = bodyFrame == null ? null : new BodyFrameStreamOne.BodyFrameOne(bodyFrame);
                var depthFrameOne = depthFrame == null ? null : new DepthFrameStreamOne.DepthFrameOne(depthFrame);
                MultiFrameArrived?.Invoke(this, new MultiFrameReadyEventArgs(new MultiFrameOne(colorFrameOne, bodyFrameOne, depthFrameOne)));
            }
        }

        private readonly KinectOne _kinectOne;
        private readonly MultiSourceFrameReader _multiReader;

        public void Dispose()
        {
            _multiReader.MultiSourceFrameArrived -= MultiReaderOnMultiSourceFrameArrived;
            _multiReader?.Dispose();
        }
    }

    public class MultiFrameOne : IMultiFrame
    {
        public MultiFrameOne(ColorFrameStreamOne.ColorFrameOne colorFrame, BodyFrameStreamOne.BodyFrameOne bodyFrame, DepthFrameStreamOne.DepthFrameOne depthFrame)
        {
            _bodyFrame = bodyFrame;
            _colorFrame = colorFrame;
            _depthFrame = depthFrame;
        }

        private readonly ColorFrameStreamOne.ColorFrameOne _colorFrame;
        private readonly BodyFrameStreamOne.BodyFrameOne _bodyFrame;
        private readonly DepthFrameStreamOne.DepthFrameOne _depthFrame;

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
