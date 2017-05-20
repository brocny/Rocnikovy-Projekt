using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KinectUnifier;
using Microsoft.Kinect;
using JointType = KinectUnifier.JointType;


namespace Kinect360
{
    public class Kinect360 : IKinect
    {
        public KinectSensor KinectSensor { get; private set; }
        public bool IsKinectOne => false;

        public IBodyManager BodyManager => _bodyManager;
        public IColorManager ColorManager => _colorManager;
        public ICoordinateMapper CoordinateMapper => _coordinateMapper ?? (_coordinateMapper = new CoordinateMapper360(KinectSensor.CoordinateMapper));

        public bool IsRunning => KinectSensor.IsRunning;
        

        private BodyManager360 _bodyManager;
        private ColorManager360 _colorManager;
        private CoordinateMapper360 _coordinateMapper;
        

        

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
                _colorManager = new ColorManager360(this);
            
        }

        public void OpenBodyManager()
        {
            if(_bodyManager == null)
                _bodyManager = new BodyManager360(this);
        }

        
    }

    class BodyManager360 : IBodyManager
    {
        private Kinect360 _kinect360;
        private SkeletonStream _skeletonStream;
        public int BodyCount => _skeletonStream.FrameSkeletonArrayLength;

        
        public BodyManager360(Kinect360 kinect360)
        {
            _kinect360 = kinect360;
            _kinect360.KinectSensor.SkeletonStream.Enable();
            _skeletonStream = _kinect360.KinectSensor.SkeletonStream;
            _kinect360.KinectSensor.SkeletonFrameReady += KinectSensor_SkeletonFrameReady;
        }

        public event EventHandler<BodyFrameReadyEventArgs> BodyFrameReady;

        private void KinectSensor_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            var skeletonFrame = e.OpenSkeletonFrame();
            BodyFrameReady?.Invoke(this, new BodyFrameReadyEventArgs(new BodyFrame360(skeletonFrame)));
        }

        public class BodyFrame360 : IBodyFrame
        {
            private SkeletonFrame _skeletonFrame;

            public BodyFrame360(SkeletonFrame skeletonFrame)
            {
                _skeletonFrame = skeletonFrame;
            }

            public void CopyBodiesTo(IBody[] bodies)
            {
                var skeletonData = new Skeleton[_skeletonFrame.SkeletonArrayLength];
                _skeletonFrame.CopySkeletonDataTo(skeletonData);
                for (int i = 0; i < bodies.Length && i < skeletonData.Length; i++)
                {
                    bodies[i] = new Body360(skeletonData[i]);
                    
                }
            }

            #region IDisposable Support
            private bool disposedValue = false; // To detect redundant calls

            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        _skeletonFrame.Dispose();
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

        public class Body360 : IBody
        {
            private Skeleton _body;

            public Body360(Skeleton body)
            {
                _body = body;
                _joints = new Dictionary<KinectUnifier.JointType, IJoint>(21);
                for (int i = 0; i <= 20; i++)
                {
                    _joints.Add((KinectUnifier.JointType)i, new Joint360(_body.Joints[(Microsoft.Kinect.JointType)i]));
                }
            }
            
            public IReadOnlyDictionary<KinectUnifier.JointType, IJoint> Joints => _joints;
            private Dictionary<KinectUnifier.JointType, IJoint> _joints;
            

            public bool IsTracked => _body.TrackingState == SkeletonTrackingState.Tracked;
        }

        public class Joint360 : IJoint
        {
            private Joint _joint;

            public Joint360(Joint joint)
            {
                _joint = joint;
            }

            public Point3F CameraSpacePoint => new Point3F(_joint.Position.X, _joint.Position.Y, _joint.Position.Z);
        }

        
    }

    class ColorManager360 : IColorManager
    {
        private Kinect360 _kinect360;
        private ColorImageStream _colorStream;

        public ColorManager360(Kinect360 kinect360)
        {
            _kinect360 = kinect360;
            _kinect360.KinectSensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
            _colorStream = _kinect360.KinectSensor.ColorStream;
            _kinect360.KinectSensor.ColorFrameReady += KinectSensor_ColorFrameReady;
        }

        private void KinectSensor_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            var colorImageFrame = e.OpenColorImageFrame();
            ColorFrameReady?.Invoke(this, new ColorFrameReadyEventArgs(new ColorFrame360(colorImageFrame)));
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

    public class CoordinateMapper360 : ICoordinateMapper
    {
        private CoordinateMapper _coordinateMapper;
        public CoordinateMapper360(CoordinateMapper coordinateMapper)
        {
            _coordinateMapper = coordinateMapper;
        }

        public void MapCameraPointsToColorSpace(Point3F[] cameraPoints, Point2F colorPoints)
        {
            throw new NotImplementedException();
        }

        public void MapCameraPointsToDepthSpace(Point3F[] cameraPoints, Point2F[] depthPoints)
        {
            throw new NotImplementedException();
        }

        public Point2F MapCameraPointToColorSpace(Point3F cameraPoint)
        {
            var colorSpacePoint =
               _coordinateMapper.MapSkeletonPointToColorPoint(new SkeletonPoint()
               {
                   X = cameraPoint.X,
                   Y = cameraPoint.Y,
                   Z = cameraPoint.Z
               }, ColorImageFormat.RgbResolution640x480Fps30);
            return new Point2F(colorSpacePoint.X, colorSpacePoint.Y);
        }

        public Point2F MapCameraPointToDepthSpace(Point3F cameraPoint)
        {
            var colorSpacePoint =
               _coordinateMapper.MapSkeletonPointToDepthPoint(new SkeletonPoint()
               {
                   X = cameraPoint.X,
                   Y = cameraPoint.Y,
                   Z = cameraPoint.Z
               }, DepthImageFormat.Resolution640x480Fps30);
            return new Point2F(colorSpacePoint.X, colorSpacePoint.Y);
        }
    }


}