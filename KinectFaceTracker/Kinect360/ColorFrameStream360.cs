using System;
using Core.Kinect;
using Microsoft.Kinect;


namespace Kinect360
{
    public class ColorFrameStream360 : IColorFrameStream
    {
        private readonly Kinect360 _kinect360;
        private readonly ColorImageStream _colorStream;

        internal ColorImageFormat ColorImageFormat { get; set; } = ColorImageFormat.RgbResolution1280x960Fps12;

        public ColorFrameStream360(Kinect360 kinect360)
        {
            _kinect360 = kinect360;
            _colorStream = _kinect360.KinectSensor.ColorStream;
        }

        public bool IsOpen => _colorStream.IsEnabled;
        public int FrameWidth => ColorImageFormat == ColorImageFormat.RgbResolution640x480Fps30 ? 640 : 1280;
        public int FrameHeight => ColorImageFormat == ColorImageFormat.RgbResolution640x480Fps30 ? 480 : 960;
        public int BytesPerPixel => _colorStream.FrameBytesPerPixel;
        public int FrameDataSize => _colorStream.FramePixelDataLength;

        private void KinectSensor_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            var colorImageFrame = e.OpenColorImageFrame();
            if (colorImageFrame != null)
            {
                _colorFrameReady?.Invoke(this, new ColorFrameReadyEventArgs(new ColorFrame360(colorImageFrame)));
            }
        }

        private bool _isEventRegistered = false;
        private EventHandler<ColorFrameReadyEventArgs> _colorFrameReady;
        public event EventHandler<ColorFrameReadyEventArgs> ColorFrameReady
        {
            add
            {
                if (!_isEventRegistered)
                {
                    _kinect360.KinectSensor.ColorFrameReady += KinectSensor_ColorFrameReady;
                    _isEventRegistered = true;
                }
                _colorFrameReady += value;
            }
            remove
            {
                _colorFrameReady -= value;
                if (_colorFrameReady == null && _isEventRegistered)
                {
                    _kinect360.KinectSensor.ColorFrameReady -= KinectSensor_ColorFrameReady;
                    _isEventRegistered = false;
                }
            }
        }

        public IColorFrame GetNextFrame()
        {
            var colorFrame = _colorStream.OpenNextFrame(30);
            return colorFrame == null ? null : new ColorFrame360(colorFrame);
        }

        public void Open(bool preferResolutionOverFps)
        {
            ColorImageFormat = preferResolutionOverFps
                ? ColorImageFormat.RgbResolution1280x960Fps12
                : ColorImageFormat.RgbResolution640x480Fps30;
            _kinect360.KinectSensor.ColorStream.Enable(ColorImageFormat);
            if (_colorFrameReady != null && !_isEventRegistered)
            {
                _kinect360.KinectSensor.ColorFrameReady += KinectSensor_ColorFrameReady;
                _isEventRegistered = true;
            }
        }

        public void Close()
        {
            _kinect360.KinectSensor.ColorStream.Disable();
            if (_isEventRegistered)
            {
                _kinect360.KinectSensor.ColorFrameReady -= KinectSensor_ColorFrameReady;
                _isEventRegistered = false;
            }
        }

        public class ColorFrame360 : IColorFrame
        {
            public ColorFrame360(ColorImageFrame colorImageFrame)
            {
                _colorImageFrame = colorImageFrame ?? throw new ArgumentNullException(nameof(colorImageFrame));
            }

            private readonly ColorImageFrame _colorImageFrame;

            public int BytesPerPixel => _colorImageFrame.BytesPerPixel;

            public int PixelDataLength => _colorImageFrame.PixelDataLength;

            public int Height => _colorImageFrame.Height;

            public int Width => _colorImageFrame.Width;

            public void CopyFramePixelDataToArray(byte[] buffer)
            {
                _colorImageFrame.CopyPixelDataTo(buffer);
            }

            public void CopyFramePixelDataToIntPtr(IntPtr ptr, int pixelDataLength)
            {
                _colorImageFrame.CopyPixelDataTo(ptr, pixelDataLength);
            }

            public void Dispose()
            {
                _colorImageFrame?.Dispose();
            }
        }
    }


}