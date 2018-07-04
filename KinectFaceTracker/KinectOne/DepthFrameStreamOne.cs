using System;
using Core.Kinect;
using Microsoft.Kinect;

namespace KinectOne
{
    public class DepthFrameStreamOne : IDepthFrameStream
    {
        public DepthFrameStreamOne(KinectOne kinectOne)
        {
            _depthFrameSource = kinectOne.KinectSensor.DepthFrameSource;
        }

        public void Open()
        {
            lock (_eventLock)
            {
                if (_depthFrameReader == null)
                {
                    _depthFrameReader = _depthFrameSource.OpenReader();
                    if (!_isEventRegistered && _depthFrameReady != null)
                    {
                        _depthFrameReader.FrameArrived += DepthFrameReaderOnFrameArrived;
                    }
                }
                else
                {
                    _depthFrameReader.IsPaused = false;
                }
            }
        }

        public void Close()
        {
            lock (_eventLock)
            {
                if (_depthFrameReader != null)
                {
                    _depthFrameReader.IsPaused = true;
                }
            }
        }

        public int FrameWidth => _depthFrameSource.FrameDescription.Width;
        public int FrameHeight => _depthFrameSource.FrameDescription.Height;
        public int MinDistance => _depthFrameSource.DepthMinReliableDistance;
        public int MaxDistance => _depthFrameSource.DepthMaxReliableDistance;

        public bool IsOpen => !_depthFrameReader.IsPaused;
        private bool _isEventRegistered;
        private EventHandler<DepthFrameReadyEventArgs> _depthFrameReady;
        private readonly object _eventLock = new object();
        public event EventHandler<DepthFrameReadyEventArgs> DepthFrameReady
        {
            add
            {
                lock (_eventLock)
                {
                    if (_depthFrameReader != null && !_isEventRegistered)
                    {
                        _depthFrameReader.FrameArrived += DepthFrameReaderOnFrameArrived;
                        _isEventRegistered = true;
                    }

                }
                _depthFrameReady += value;
            }
            remove
            {
                _depthFrameReady -= value;
                lock (_eventLock)
                {
                    if (_depthFrameReady == null && _depthFrameReader != null && _isEventRegistered)
                    {
                        _depthFrameReader.FrameArrived -= DepthFrameReaderOnFrameArrived;
                    }
                }
            }

        }

        private void DepthFrameReaderOnFrameArrived(object sender, DepthFrameArrivedEventArgs e)
        {
            var depthFrame = e.FrameReference.AcquireFrame();
            if (depthFrame != null)
            {
                _depthFrameReady?.Invoke(this, new DepthFrameReadyEventArgs(new DepthFrame(depthFrame)));
            }
        }

        public IDepthFrame GetNextFrame()
        {
            var frame = _depthFrameReader.AcquireLatestFrame();
            return frame == null ? null : new DepthFrame(frame);
        }

        private readonly DepthFrameSource _depthFrameSource;
        private DepthFrameReader _depthFrameReader;

        public class DepthFrame : IDepthFrame
        {
            public DepthFrame(Microsoft.Kinect.DepthFrame depthFrame)
            {
                _depthFrame = depthFrame ?? throw new ArgumentNullException(nameof(depthFrame));
            }

            public void Dispose()
            {
                _depthFrame.Dispose();
            }

            public int Width => _depthFrame.FrameDescription.Width;
            public int Height => _depthFrame.FrameDescription.Height;
            public void CopyFramePixelDataToArray(ushort[] array)
            {
                _depthFrame.CopyFrameDataToArray(array);
            }

            public void CopyFramePixelDataToIntPtr(IntPtr ptr, int size)
            {
                _depthFrame.CopyFrameDataToIntPtr(ptr, (uint) size);
            }

            public int BytesPerPixel => (int) _depthFrame.FrameDescription.BytesPerPixel;

            private readonly Microsoft.Kinect.DepthFrame _depthFrame;
        }
    }
}
