using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core;
using Microsoft.Kinect;

namespace KinectOne
{
    public class MultiFrameManagerOne : IMultiManager
    {
        public MultiFrameManagerOne(MultiFrameTypes frameTypes, KinectOne kinect)
        {
            FrameTypes = frameTypes;
            _kinectOne = kinect;
            _multiReader = _kinectOne.KinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Body | FrameSourceTypes.Color);
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
                var colorFrameOne = colorFrame == null ? null : new ColorManagerOne.ColorFrameOne(colorFrame);
                var bodyFrameOne = bodyFrame == null ? null : new BodyManagerOne.BodyFrameOne(bodyFrame);
                MultiFrameArrived?.Invoke(this, new MultiFrameReadyEventArgs(new MultiFrameOne(colorFrameOne, bodyFrameOne)));
            }
        }

        private KinectOne _kinectOne;
        private MultiSourceFrameReader _multiReader;

        public void Dispose()
        {
            _multiReader.MultiSourceFrameArrived -= MultiReaderOnMultiSourceFrameArrived;
            _multiReader?.Dispose();
        }
    }

    public class MultiFrameOne : IMultiFrame
    {
        public MultiFrameOne(ColorManagerOne.ColorFrameOne colorFrame, BodyManagerOne.BodyFrameOne bodyFrame)
        {
            _bodyFrame = bodyFrame;
            _colorFrame = colorFrame;
        }

        private ColorManagerOne.ColorFrameOne _colorFrame;
        private BodyManagerOne.BodyFrameOne _bodyFrame;

        public IColorFrame ColorFrame => _colorFrame;

        public IBodyFrame BodyFrame => _bodyFrame;

        public void Dispose()
        {
            _colorFrame?.Dispose();
            _bodyFrame?.Dispose();
        }
    }
}
