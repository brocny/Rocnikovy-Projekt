using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KinectUnifier;
using Microsoft.Kinect;


namespace Kinect360
{
    public class Kinect360 : IKinect
    {
        public KinectSensor KinectSensor { get; private set; }

        public IBodyManager BodyManager => _bodyManager;
        public IColorManager ColorManager => _colorManager;

        public bool IsRunning => KinectSensor.IsRunning;
        

        private BodyManager360 _bodyManager;
        private ColorManager360 _colorManager;

        

        public Kinect360()
        {
            KinectSensor = KinectSensor.KinectSensors.FirstOrDefault(x => x.Status == KinectStatus.Connected);
        }

        public void Open()
        {
            KinectSensor.Start();
        }

        public void OpenColorManager()
        {
            if(_colorManager == null)
                _colorManager = new ColorManager360();
            
        }

        public void OpenBodyManager()
        {
            if(_bodyManager == null)
                _bodyManager = new BodyManager360();
        }
    }

    class BodyManager360 : IBodyManager
    {
        private Kinect360 _kinect;
        private SkeletonStream _skeletonStream;



        public BodyManager360()
        {
            _kinect.KinectSensor.SkeletonStream.Enable();
            _skeletonStream = _kinect.KinectSensor.SkeletonStream;
            _kinect.KinectSensor.SkeletonFrameReady += KinectSensor_SkeletonFrameReady;
        }

        public event EventHandler<BodyFrameReadyEventArgs> BodyFrameReady;

        private void KinectSensor_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            throw new NotImplementedException();
        }
    }

    class ColorManager360 : IColorManager
    {
        private Kinect360 _kinect;
        private ColorImageStream _colorStream;

        public ColorManager360()
        {
            _kinect.KinectSensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
            _colorStream = _kinect.KinectSensor.ColorStream;
            _kinect.KinectSensor.ColorFrameReady += KinectSensor_ColorFrameReady;
        }

        private void KinectSensor_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (var colorFrame = e.OpenColorImageFrame())
            {
                
            }
        }

        public event EventHandler<ColorFrameReadyEventArgs> ColorFrameReady;

        public class ColorFrame360 : IColorFrame
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