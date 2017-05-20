using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using KinectUnifier;
using Microsoft.Kinect;
using JointType = KinectUnifier.JointType;

namespace Kinect_Test
{
    public class KinectOne : IKinect
    {
        public KinectSensor KinectSensor { get; private set; }

        public bool IsKinectOne => true;

        public IBodyManager BodyManager => _bodyManager;
        public IColorManager ColorManager => _colorManager;
        public ICoordinateMapper CoordinateMapper => _coordinateMapper ?? (_coordinateMapper = new CoordinateMapperOne(KinectSensor.CoordinateMapper));

        public bool IsRunning => KinectSensor.IsOpen;

        private BodyManagerOne _bodyManager;
        private ColorManagerOne _colorManager;
        private CoordinateMapperOne _coordinateMapper;
        

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
            _bodyManager = new BodyManagerOne(this);
        }

        public void OpenColorManager()
        {
            _colorManager = new ColorManagerOne(this);
        }
    }

    public class BodyManagerOne : IBodyManager
    {
        private KinectOne _kinectOne;

        private BodyFrameReader bodyFrameReader;
        private BodyFrameSource bodyFrameSource;

        public int BodyCount => bodyFrameSource.BodyCount;

        public BodyManagerOne(KinectOne kinectOne)
        {
            _kinectOne = kinectOne;
            bodyFrameSource = _kinectOne.KinectSensor.BodyFrameSource;
            bodyFrameReader = bodyFrameSource.OpenReader();

            bodyFrameReader.FrameArrived += BodyFrameReader_FrameArrived;
        }

        public event EventHandler<BodyFrameReadyEventArgs> BodyFrameReady;

        private void BodyFrameReader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            var bodyFrame = e.FrameReference.AcquireFrame();
            BodyFrameReady?.Invoke(this, new BodyFrameReadyEventArgs(new BodyFrameOne(bodyFrame)));
        }

        public class BodyFrameOne : IBodyFrame
        {
            private BodyFrame _bodyFrame;

            public BodyFrameOne(BodyFrame bodyFrame)
            {
                _bodyFrame = bodyFrame;
            }

            public void CopyBodiesTo(IBody[] bodies)
            {
                var bodyData = new Body[_bodyFrame.BodyCount];
                _bodyFrame.GetAndRefreshBodyData(bodyData);
                for (int i = 0; i < bodyData.Length && i < bodies.Length; i++)
                {
                    bodies[i] = new BodyOne(bodyData[i]);
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
                        _bodyFrame.Dispose();
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

        public class BodyOne : IBody
        {
            private Body _body;

            public BodyOne(Body body)
            {
                _body = body;
                _joints = new Dictionary<JointType, IJoint>(21);
                for (int i = 0; i <= 20; i++)
                {
                    _joints.Add((KinectUnifier.JointType)i, new JointOne(_body.Joints[(Microsoft.Kinect.JointType)i]));
                }
            }

            public IReadOnlyDictionary<KinectUnifier.JointType, IJoint> Joints => _joints;
            private Dictionary<KinectUnifier.JointType, IJoint> _joints;

            public bool IsTracked => _body.IsTracked;
        }

        public class JointOne : IJoint
        {
            private Joint _joint;
            public Point3F CameraSpacePoint => new Point3F(_joint.Position.X, _joint.Position.Y, _joint.Position.Z);

            public JointOne(Joint joint)
            {
                _joint = joint;
            }
        }

    }

    public class ColorManagerOne : IColorManager
    {
        private KinectOne _kinectOne;

        private ColorFrameReader colorFrameReader;
        private ColorFrameSource colorFrameSource;

       

        public ColorManagerOne(KinectOne kinectOneOne)
        {
            _kinectOne = kinectOneOne;
            colorFrameSource = _kinectOne.KinectSensor.ColorFrameSource;
            colorFrameReader = colorFrameSource.OpenReader();
            colorFrameReader.FrameArrived += ColorFrameReader_FrameArrived;
        }

        private void ColorFrameReader_FrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            var colorFrame = e.FrameReference.AcquireFrame();
            ColorFrameReady?.Invoke(this, new ColorFrameReadyEventArgs(new ColorFrameOne(colorFrame)));
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

    public class CoordinateMapperOne : ICoordinateMapper
    {
        private CoordinateMapper _coordinateMapper;

        public CoordinateMapperOne(CoordinateMapper coordinateMapper)
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
                _coordinateMapper.MapCameraPointToColorSpace(new CameraSpacePoint()
                {
                    X = cameraPoint.X,
                    Y = cameraPoint.Y,
                    Z = cameraPoint.Z
                });
            return new Point2F(colorSpacePoint.X, colorSpacePoint.Y);
        }

        public Point2F MapCameraPointToDepthSpace(Point3F cameraPoint)
        {
            var depthSpacePoint = _coordinateMapper.MapCameraPointToDepthSpace(new CameraSpacePoint()
            {
                X = cameraPoint.X,
                Y = cameraPoint.Y,
                Z = cameraPoint.Z
            });
            return new Point2F(depthSpacePoint.X, depthSpacePoint.Y);
        }
    }



}
