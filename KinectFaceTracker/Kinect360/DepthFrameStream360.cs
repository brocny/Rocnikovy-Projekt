using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Configuration;
using System.Text;
using System.Threading.Tasks;
using Core.Kinect;
using Microsoft.Kinect;

namespace Kinect360
{
    public class DepthFrameStream360 : IDepthFrameStream
    {
        public DepthFrameStream360(Kinect360 kinect360)
        {
            _kinect360 = kinect360;
            _depthImageStream = kinect360.KinectSensor.DepthStream;
        }

        public void Open()
        {
            _depthImageStream.Enable();
            lock (_eventLock)
            {
                if (_depthFrameReady != null && !_isEventRegistered)
                {
                    _kinect360.KinectSensor.DepthFrameReady += KinectSensorOnDepthFrameReady;
                    _isEventRegistered = true;
                }
            }
            
        }

        private void KinectSensorOnDepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            var depthFrame = e.OpenDepthImageFrame();
            if (depthFrame != null)
            {
                _depthFrameReady?.Invoke(this, new DepthFrameReadyEventArgs(new DepthFrame(depthFrame)));
            }
        }

        public void Close()
        {
            _kinect360.KinectSensor.DepthStream.Disable();
            lock (_eventLock)
            {
                if (_isEventRegistered)
                {
                    _kinect360.KinectSensor.DepthFrameReady -= KinectSensorOnDepthFrameReady;
                    _isEventRegistered = false;
                }
            }
            
        }

        public int FrameWidth => _depthImageStream.FrameWidth;
        public int FrameHeight => _depthImageStream.FrameHeight;
        public int MinDistance => _depthImageStream.MinDepth;
        public int MaxDistance => _depthImageStream.MaxDepth;
        public bool IsOpen => _depthImageStream.IsEnabled;

        private bool _isEventRegistered = false;
        private EventHandler<DepthFrameReadyEventArgs> _depthFrameReady;
        private readonly object _eventLock = new object();
        public event EventHandler<DepthFrameReadyEventArgs> DepthFrameReady
        {
            add
            {
                lock (_eventLock)
                {
                    if (!_isEventRegistered)
                    {
                        _kinect360.KinectSensor.DepthFrameReady += KinectSensorOnDepthFrameReady;
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
                    if (_depthFrameReady == null && _isEventRegistered)
                    {
                        _kinect360.KinectSensor.DepthFrameReady -= KinectSensorOnDepthFrameReady;
                    }
                }
            }
        }
        public IDepthFrame GetNextFrame()
        {
            var depthFrame = _depthImageStream.OpenNextFrame(30);
            return depthFrame == null ? null : new DepthFrame(depthFrame);
        }

        private readonly DepthImageStream _depthImageStream;
        private readonly Kinect360 _kinect360;

        public class DepthFrame : IDepthFrame
        {
            public DepthFrame(DepthImageFrame depthImageFrame)
            {
                _depthImageFrame = depthImageFrame ?? throw new ArgumentNullException(nameof(depthImageFrame));
            }

            public void Dispose()
            {
                _depthImageFrame?.Dispose();
            }

            public int Width => _depthImageFrame.Width;
            public int Height => _depthImageFrame.Height;
            public void CopyFramePixelDataToArray(ushort[] array)
            {
                var rawPixelData = _depthImageFrame.GetRawPixelData();
                for (int i = 0; i < rawPixelData.Length; i++)
                {
                    array[i] = (ushort) rawPixelData[i].Depth;
                }
            }

            public void CopyFramePixelDataToIntPtr(IntPtr ptr, int size)
            {
                _depthImageFrame.CopyPixelDataTo(ptr, size);
            }

            private readonly DepthImageFrame _depthImageFrame;
        }
    }
}
