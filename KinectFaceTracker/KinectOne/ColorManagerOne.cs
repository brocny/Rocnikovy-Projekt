using System;
using KinectUnifier;
using Microsoft.Kinect;

namespace KinectOne
{
    public class ColorManagerOne : IColorManager
    {

        public int WidthPixels => _colorFrameSource.FrameDescription.Width;
        public int HeightPixels => _colorFrameSource.FrameDescription.Height;
        public int BytesPerPixel => (int) _colorFrameSource.FrameDescription.BytesPerPixel;

        public ColorManagerOne(KinectOne kinectOne)
        {
            _colorFrameSource = kinectOne.KinectSensor.ColorFrameSource;
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

        private ColorFrameReader _colorFrameReader;
        private readonly ColorFrameSource _colorFrameSource;

        private void ColorFrameReader_FrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            var colorFrame = e.FrameReference.AcquireFrame();
            if (colorFrame != null)
            {
                ColorFrameReady?.Invoke(this, new ColorFrameReadyEventArgs(new ColorFrameOne(colorFrame)));
            }
        }

        public event EventHandler<ColorFrameReadyEventArgs> ColorFrameReady;

        public class ColorFrameOne : IColorFrame
        {
            public ColorFrameOne(ColorFrame colorFrame)
            {
                _colorFrame = colorFrame;
            }

            private readonly ColorFrame _colorFrame;

            public int BytesPerPixel => 4; // Change when changing the ColorImageFormat!

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
