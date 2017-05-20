using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KinectUnifier;
using Microsoft.Kinect;

namespace Kinect_Test
{
    public class KinectOne : IKinect
    {
        public KinectSensor KinectSensor { get; private set; }

        public IBodyManager BodyManager => _bodyManager;
        public IColorManager ColorManager => _colorManager;

        public bool IsRunning => KinectSensor.IsOpen;

        private BodyManagerOne _bodyManager;
        private ColorManagerOne _colorManager;
        

        public KinectOne()
        {
            KinectSensor = KinectSensor.GetDefault();
        }

        public void Open()
        {
            KinectSensor.Open();
        }



        public void OpenBodyManager()
        {
            _bodyManager = new BodyManagerOne();
        }

        public void OpenColorManager()
        {
            _colorManager = new ColorManagerOne();
        }
    }

    public class BodyManagerOne : IBodyManager
    {
        private KinectOne KinectOne;

        private BodyFrameReader bodyFrameReader;
        private BodyFrameSource bodyFrameSource;

        public BodyManagerOne()
        {
            bodyFrameSource = KinectOne.KinectSensor.BodyFrameSource;
            bodyFrameReader = bodyFrameSource.OpenReader();

            bodyFrameReader.FrameArrived += BodyFrameReader_FrameArrived;
        }

        public event EventHandler<BodyFrameReadyEventArgs> BodyFrameReady;

        private void BodyFrameReader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            throw new NotImplementedException();
        }


    }

    public class ColorManagerOne : IColorManager
    {
        private KinectOne KinectOne;

        private ColorFrameReader colorFrameReader;
        private ColorFrameSource colorFrameSource;

       

        public ColorManagerOne()
        {
            colorFrameSource = KinectOne.KinectSensor.ColorFrameSource;
            colorFrameReader = colorFrameSource.OpenReader();
            colorFrameReader.FrameArrived += ColorFrameReader_FrameArrived;
        }

        private void ColorFrameReader_FrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            using (var colorFrame = e.FrameReference.AcquireFrame())
            {
                
            }
            ColorFrameReady?.Invoke(this, new ColorFrameReadyEventArgs());
        }
        

        public event EventHandler<ColorFrameReadyEventArgs> ColorFrameReady;

        public class ColorFrameOne : IColorFrame
        {
            private ColorFrame _colorFrame;

            public int BytesPerPixel => 4;

            public int PixelDataLength => 4 * _colorFrame.FrameDescription.Height * _colorFrame.FrameDescription.Width;

            public int Height => _colorFrame.FrameDescription.Height;

            public int Width => _colorFrame.FrameDescription.Width;

            public void CopyFramePixelDataToArray(byte[] buffer)
            {
                _colorFrame.CopyConvertedFrameDataToArray(buffer, ColorImageFormat.Rgba);
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
