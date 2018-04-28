using System;
using Core;
using Microsoft.Kinect;


namespace Kinect360
{
    internal class ColorManager360 : IColorManager
    {
        private Kinect360 _kinect360;
        private ColorImageStream _colorStream;

        internal ColorImageFormat ColorImageFormat;

        public ColorManager360(Kinect360 kinect360)
        {
            _kinect360 = kinect360;
            _colorStream = _kinect360.KinectSensor.ColorStream;
        }

        public int FrameWidth => ColorImageFormat == ColorImageFormat.RgbResolution640x480Fps30 ? 640 : 1280;
        public int FrameHeight => ColorImageFormat == ColorImageFormat.RgbResolution640x480Fps30 ? 480 : 960;
        public int BytesPerPixel => _colorStream.FrameBytesPerPixel;
        public int FrameDataSize => _colorStream.FramePixelDataLength;


        private void KinectSensor_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            var colorImageFrame = e.OpenColorImageFrame();
            if (colorImageFrame != null)
            {
                ColorFrameReady?.Invoke(this, new ColorFrameReadyEventArgs(new ColorFrame360(colorImageFrame)));
            }
        }

        public event EventHandler<ColorFrameReadyEventArgs> ColorFrameReady;

        public IColorFrame GetNextFrame()
        {
            return new ColorFrame360(_colorStream.OpenNextFrame(30));
        }

        public void Open(bool preferResolutionOverFps)
        {
            ColorImageFormat = preferResolutionOverFps
                ? ColorImageFormat.RgbResolution1280x960Fps12
                : ColorImageFormat.RgbResolution640x480Fps30;
            _kinect360.KinectSensor.ColorStream.Enable(ColorImageFormat);
            _kinect360.KinectSensor.ColorFrameReady += KinectSensor_ColorFrameReady;
        }

        public void Close()
        {
            _kinect360.KinectSensor.ColorStream.Disable();
            _kinect360.KinectSensor.ColorFrameReady -= KinectSensor_ColorFrameReady;
        }

        internal class ColorFrame360 : IColorFrame
        {
            public ColorFrame360(ColorImageFrame colorImageFrame)
            {
                _colorImageFrame = colorImageFrame;
            }

            private ColorImageFrame _colorImageFrame;

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

            #region IDisposable Support
            private bool disposedValue = false; 

            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        _colorImageFrame.Dispose();
                    }

                    disposedValue = true;
                }
            }

            
            public void Dispose()
            {
              
                Dispose(true);
                
            }
            #endregion
        }
    }


}