using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KinectUnifier;
using Microsoft.Kinect;

namespace KinectOne
{
    public class MultiFrameManagerOne : IMultiManager
    {
        public MultiFrameManagerOne(MultiFrameTypes frameTypes, KinectOne kinect)
        {
            FrameTypes = frameTypes;
            _kinectOne = kinect;
            _multiReader = _kinectOne.KinectSensor.OpenMultiSourceFrameReader((FrameSourceTypes) frameTypes);
            _multiReader.MultiSourceFrameArrived += MultiReaderOnMultiSourceFrameArrived;
        }

        public event EventHandler<MultiFrameReadyEventArgs> MultiFrameArrived;
        public MultiFrameTypes FrameTypes { get; }

        private void MultiReaderOnMultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            var frame = e.FrameReference.AcquireFrame();
            if (frame != null)
            {
                MultiFrameArrived?.Invoke(this, new MultiFrameReadyEventArgs(new MultiFrameOne(frame)));
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
        public MultiFrameOne(MultiSourceFrame frame)
        {
            _multiSourceFrame = frame;
        }

        public IColorFrame ColorFrame
        {
            get
            {
                var cFrame = _multiSourceFrame.ColorFrameReference.AcquireFrame();
                if (cFrame != null)
                {
                    return new ColorManagerOne.ColorFrameOne(cFrame);
                }

                return null;
            }
        }

        public IBodyFrame BodyFrame
        {
            get
            {
                var bFrame = _multiSourceFrame.BodyFrameReference.AcquireFrame();
                if (bFrame != null)
                {
                    return new BodyManagerOne.BodyFrameOne(bFrame);
                }
                return null;
            }
        }

        private MultiSourceFrame _multiSourceFrame;
    }
}
