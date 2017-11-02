using System;
using KinectUnifier;
using Microsoft.Kinect;

namespace KinectOne
{
    public class ColorManagerOne : IColorManager
    {
        private KinectOne _kinectOne;

        private ColorFrameReader _colorFrameReader;
        private ColorFrameSource _colorFrameSource;

        public int WidthPixels => _colorFrameSource.FrameDescription.Width;
        public int HeightPixels => _colorFrameSource.FrameDescription.Height;
        public int BytesPerPixel => 4;

        public ColorManagerOne(KinectOne kinectOneOne)
        {
            _kinectOne = kinectOneOne;
            _colorFrameSource = _kinectOne.KinectSensor.ColorFrameSource;
            
        }

        private void ColorFrameReader_FrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            var colorFrame = e.FrameReference.AcquireFrame();
            if (colorFrame != null)
            {
                ColorFrameReady?.Invoke(this, new ColorFrameReadyEventArgs(new ColorFrameOne(colorFrame)));
            }
            
        }

        public void Open(bool preferResolutionOverFps)
        {
            _colorFrameReader = _colorFrameSource.OpenReader();
            _colorFrameReader.FrameArrived += ColorFrameReader_FrameArrived;
        }

        public void Close()
        {
            _colorFrameReader.Dispose();
        }

        public event EventHandler<ColorFrameReadyEventArgs> ColorFrameReady;

        public class ColorFrameOne : IColorFrame
        {
            public ColorFrameOne(ColorFrame colorFrame)
            {
                _colorFrame = colorFrame;
            }

            private ColorFrame _colorFrame;

            public int BytesPerPixel => 4;

            public int PixelDataLength => 4 * _colorFrame.FrameDescription.Height * _colorFrame.FrameDescription.Width;

            public int Height => _colorFrame.FrameDescription.Height;

            public int Width => _colorFrame.FrameDescription.Width;

            public void CopyFramePixelDataToArray(byte[] buffer)
            {
                _colorFrame.CopyConvertedFrameDataToArray(buffer, ColorImageFormat.Rgba);
            }

            public void CopyFramePixelDataToIntPtr(IntPtr ptr, int pixelDataLength)
            {
                _colorFrame.CopyConvertedFrameDataToIntPtr(ptr, (uint)pixelDataLength, ColorImageFormat.Bgra);
            }

            #region IDisposable Support
            private bool disposedValue = false; // To detect redundant calls

            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        _colorFrame.Dispose();
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
