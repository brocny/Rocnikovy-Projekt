using System;
using Core.Kinect;
using Microsoft.Kinect;

namespace KinectOne
{
    public class ColorFrameStreamOne : IColorFrameStream
    {
        public bool IsOpen => !_colorFrameReader.IsPaused;
        public int FrameWidth => _frameDescription.Width;
        public int FrameHeight => _frameDescription.Height;
        public int BytesPerPixel => (int) _frameDescription.BytesPerPixel;
        public int FrameDataSize => (int) _frameDescription.LengthInPixels * BytesPerPixel;

        public ColorFrameStreamOne(KinectOne kinectOne)
        {
            _colorFrameSource = kinectOne.KinectSensor.ColorFrameSource;
            _frameDescription = _colorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);
        }

        public IColorFrame GetNextFrame()
        {
            var frame = _colorFrameReader.AcquireLatestFrame();
            return frame == null ? null : new ColorFrame(frame);
        }

        public void Open(bool preferResolutionOverFps)
        {
            lock (_eventLock)
            {
                if (_colorFrameReader == null)
                {
                    _colorFrameReader = _colorFrameSource.OpenReader();

                    if (!_isEventRegistered && _colorFrameReady != null)
                    {
                        _colorFrameReader.FrameArrived += ColorFrameReader_FrameArrived;
                    }

                }
                else
                {
                    _colorFrameReader.IsPaused = false;
                }
            }
        }

        public void Close()
        {
            lock (_eventLock)
            {
                if (_colorFrameReader != null)
                {
                    _colorFrameReader.IsPaused = true;
                }
            }
        }

        private ColorFrameReader _colorFrameReader;
        private readonly ColorFrameSource _colorFrameSource;
        private readonly FrameDescription _frameDescription;


        private void ColorFrameReader_FrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            var colorFrame = e.FrameReference.AcquireFrame();
            if (colorFrame != null)
            {
                _colorFrameReady?.Invoke(this, new ColorFrameReadyEventArgs(new ColorFrame(colorFrame)));
            }
        }

        private bool _isEventRegistered;
        private EventHandler<ColorFrameReadyEventArgs> _colorFrameReady;
        private readonly object _eventLock = new object();
        public event EventHandler<ColorFrameReadyEventArgs> ColorFrameReady
        {
            add
            {
                lock (_eventLock)
                {
                    if (_colorFrameReader != null && !_isEventRegistered)
                    {
                        _colorFrameReader.FrameArrived += ColorFrameReader_FrameArrived;
                        _isEventRegistered = true;
                    }
                }

                _colorFrameReady += value;
                
            }
            remove
            {
                _colorFrameReady -= value;

                lock (_eventLock)
                {
                    if (_colorFrameReady == null && _colorFrameReader != null && _isEventRegistered)
                    {
                        _colorFrameReader.FrameArrived -= ColorFrameReader_FrameArrived;
                    }
                }
            }

        }

        public class ColorFrame : IColorFrame
        {
            public ColorFrame(Microsoft.Kinect.ColorFrame colorFrame)
            {
                _colorFrame = colorFrame ?? throw new ArgumentNullException(nameof(colorFrame));
            }

            private readonly Microsoft.Kinect.ColorFrame _colorFrame;

            public int BytesPerPixel => 4;

            public int PixelDataLength => _colorFrame.FrameDescription.Width * _colorFrame.FrameDescription.Height * BytesPerPixel; 

            public int Height => _colorFrame.FrameDescription.Height;

            public int Width => _colorFrame.FrameDescription.Width;

            public void CopyFramePixelDataToArray(byte[] buffer)
            {
                _colorFrame.CopyConvertedFrameDataToArray(buffer, ColorImageFormat.Bgra);
            }

            public void CopyFramePixelDataToIntPtr(IntPtr ptr, int pixelDataLength)
            {
                _colorFrame.CopyConvertedFrameDataToIntPtr(ptr, (uint)pixelDataLength, ColorImageFormat.Bgra);
            }

            public void Dispose()
            {
                _colorFrame?.Dispose();
            }
        }
    }
}
