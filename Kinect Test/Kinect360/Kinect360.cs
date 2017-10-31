using System;
using System.Collections.Generic;
using System.Linq;
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
        public ICoordinateMapper CoordinateMapper => _coordinateMapper ?? (_coordinateMapper = new CoordinateMapper360(this));

        public bool IsRunning => KinectSensor.IsRunning;
        


        private BodyManager360 _bodyManager;
        internal ColorManager360 _colorManager;
        private CoordinateMapper360 _coordinateMapper;

        private ColorImageFormat _colorImageFormat;

        public Kinect360()
        {
            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    KinectSensor = potentialSensor;
                    break;
                }
            }
            if (KinectSensor == null)
                throw new Exception("No Kinect device found!");
            
            _bodyManager = new BodyManager360(this);
            _colorManager = new ColorManager360(this);
        }

        public void Open()
        {
            KinectSensor.Start();
        }

        public void Close()
        {
            KinectSensor.Stop();
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
            
            _skeletonStream = _kinect360.KinectSensor.SkeletonStream;
            
        }

        public event EventHandler<BodyFrameReadyEventArgs> BodyFrameReady;

        private void KinectSensor_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            var skeletonFrame = e.OpenSkeletonFrame();
            if (skeletonFrame != null)
            {
                BodyFrameReady?.Invoke(this, new BodyFrameReadyEventArgs(new BodyFrame360(skeletonFrame)));
            }
        }

        public void Open()
        {
            _kinect360.KinectSensor.SkeletonStream.Enable();
            _kinect360.KinectSensor.SkeletonFrameReady += KinectSensor_SkeletonFrameReady;
        }

        public void Close()
        {
            _kinect360.KinectSensor.SkeletonStream.Disable();
            _kinect360.KinectSensor.SkeletonFrameReady -= KinectSensor_SkeletonFrameReady;
        }

        public class BodyFrame360 : IBodyFrame
        {
            private SkeletonFrame _skeletonFrame;

            public BodyFrame360(SkeletonFrame skeletonFrame)
            {
                _skeletonFrame = skeletonFrame;
            }

            public int BodyCount => _skeletonFrame.SkeletonArrayLength;

            public Point4F FloorClipPlane
            {
                get
                {
                    var fcp = _skeletonFrame.FloorClipPlane;
                    return new Point4F(fcp.Item1, fcp.Item1, fcp.Item3, fcp.Item4);
                }
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
           
            public IReadOnlyDictionary<KinectUnifier.JointType, IJoint> Joints => _joints;
            public IReadOnlyList<ValueTuple<JointType, JointType>> Bones => _bones;
            public bool IsTracked => _body.TrackingState == SkeletonTrackingState.Tracked;


            public Body360(Skeleton body)
            {
                _body = body;
                _joints = new Dictionary<KinectUnifier.JointType, IJoint>(20);

                //TODO: Do this in O(1) instead of O(n)
                for (int i = 0; i < 20; i++)
                {
                    _joints.Add(_jointNumbers[i], new Joint360(_body.Joints[(Microsoft.Kinect.JointType)i]));
                }
            }
            
            private Dictionary<KinectUnifier.JointType, IJoint> _joints;

            private Skeleton _body;
            // needed because joint numbers are slightly different in SDK 1.8 vs 2.0
            private static readonly KinectUnifier.JointType[] _jointNumbers =
            {
                // 0 : Spine + head
                JointType.HipCenter, JointType.SpineMid, JointType.ShoulderCenter, JointType.Head,
                // 4 : Left arm
                JointType.ShoulderLeft, JointType.ElbowLeft, JointType.WristLeft, JointType.HandLeft,
                // 8 : Right arm
                JointType.ShoulderRight, JointType.ElbowRight, JointType.WristRight, JointType.HandRight,
                // 12 : Left leg
                JointType.HipLeft, JointType.KneeLeft, JointType.AnkleLeft, JointType.FootLeft,
                // 16 : Right leg
                JointType.HipRight, JointType.KneeRight, JointType.AnkleRight, JointType.FootRight,
            };

            private static List<ValueTuple<JointType, JointType>> _bones = new List<ValueTuple<JointType, JointType>>
            {
                // Torso
                new ValueTuple<JointType, JointType>(JointType.Head, JointType.Neck),
                new ValueTuple<JointType, JointType>(JointType.Neck, JointType.ShoulderCenter),
                new ValueTuple<JointType, JointType>(JointType.ShoulderCenter, JointType.SpineMid),
                new ValueTuple<JointType, JointType>(JointType.SpineMid, JointType.HipCenter),
                new ValueTuple<JointType, JointType>(JointType.ShoulderCenter, JointType.ShoulderRight),
                new ValueTuple<JointType, JointType>(JointType.ShoulderCenter, JointType.ShoulderLeft),
                new ValueTuple<JointType, JointType>(JointType.HipCenter, JointType.HipRight),
                new ValueTuple<JointType, JointType>(JointType.HipCenter, JointType.HipLeft),

                // Right Arm
                new ValueTuple<JointType, JointType>(JointType.ShoulderRight, JointType.ElbowRight),
                new ValueTuple<JointType, JointType>(JointType.ElbowRight, JointType.WristRight),
                new ValueTuple<JointType, JointType>(JointType.WristRight, JointType.HandRight),

                // Left Arm
                new ValueTuple<JointType, JointType>(JointType.ShoulderLeft, JointType.ElbowLeft),
                new ValueTuple<JointType, JointType>(JointType.ElbowLeft, JointType.WristLeft),
                new ValueTuple<JointType, JointType>(JointType.WristLeft, JointType.HandLeft),

                // Right Leg
                new ValueTuple<JointType, JointType>(JointType.HipRight, JointType.KneeRight),
                new ValueTuple<JointType, JointType>(JointType.KneeRight, JointType.AnkleRight),
                new ValueTuple<JointType, JointType>(JointType.AnkleRight, JointType.FootRight),

                // Left Leg
                new ValueTuple<JointType, JointType>(JointType.HipLeft, JointType.KneeLeft),
                new ValueTuple<JointType, JointType>(JointType.KneeLeft, JointType.AnkleLeft),
                new ValueTuple<JointType, JointType>(JointType.AnkleLeft, JointType.FootLeft),

                // Left hand 
                new ValueTuple<JointType, JointType>(JointType.HandLeft, JointType.HandTipLeft),
                new ValueTuple<JointType, JointType>(JointType.HandLeft, JointType.ThumbLeft),

                // Right hand
                new ValueTuple<JointType, JointType>(JointType.HandRight, JointType.HandTipRight),
                new ValueTuple<JointType, JointType>(JointType.HandRight, JointType.ThumbRight)
        };

        }

        public class Joint360 : IJoint
        {
            private Joint _joint;

            public Joint360(Joint joint)
            {
                _joint = joint;
            }

            public bool IsTracked => _joint.TrackingState == JointTrackingState.Tracked;
            public Point3F Position => new Point3F(_joint.Position.X, _joint.Position.Y, _joint.Position.Z);
        }
    }

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

        public int WidthPixels => ColorImageFormat == ColorImageFormat.RgbResolution640x480Fps30 ? 640 : 1280;
        public int HeightPixels => ColorImageFormat == ColorImageFormat.RgbResolution640x480Fps30 ? 480 : 960;
        public int BytesPerPixel => _colorStream.FrameBytesPerPixel;

        

        private void KinectSensor_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            var colorImageFrame = e.OpenColorImageFrame();
            if (colorImageFrame != null)
            {
                ColorFrameReady?.Invoke(this, new ColorFrameReadyEventArgs(new ColorFrame360(colorImageFrame)));
            }
            
        }

        public event EventHandler<ColorFrameReadyEventArgs> ColorFrameReady;

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

    internal class CoordinateMapper360 : ICoordinateMapper
    {
        private CoordinateMapper _coordinateMapper;
        private Kinect360 _kinect360;

        public CoordinateMapper360(Kinect360 kinect360)
        {
            _kinect360 = kinect360;
            _coordinateMapper = _kinect360.KinectSensor.CoordinateMapper;
            
        }

        public void MapCameraPointsToColorSpace(Point3F[] cameraPoints, Point2F[] colorPoints)
        {
            for (int i = 0; i < cameraPoints.Length && i < colorPoints.Length; i++)
            {
                var colorSpacePoint =
                    _coordinateMapper.MapSkeletonPointToColorPoint(new SkeletonPoint()
                    {
                        X = cameraPoints[i].X,
                        Y = cameraPoints[i].Y,
                        Z = cameraPoints[i].Z
                    }, _kinect360.ColorManager == null ? ColorImageFormat.RgbResolution640x480Fps30 : _kinect360._colorManager.ColorImageFormat);

                colorPoints[i] = new Point2F(colorSpacePoint.X, colorSpacePoint.Y);
            }
        }

        public void MapCameraPointsToDepthSpace(Point3F[] cameraPoints, Point2F[] depthPoints)
        {
            for (int i = 0; i < cameraPoints.Length && i < depthPoints.Length; i++)
            {
                var colorSpacePoint =
                    _coordinateMapper.MapSkeletonPointToDepthPoint(new SkeletonPoint()
                    {
                        X = cameraPoints[i].X,
                        Y = cameraPoints[i].Y,
                        Z = cameraPoints[i].Z
                    }, DepthImageFormat.Resolution640x480Fps30);

                depthPoints[i] = new Point2F(colorSpacePoint.X, colorSpacePoint.Y);
            }
        }

        public Point2F MapCameraPointToColorSpace(Point3F cameraPoint)
        {
            var colorSpacePoint =
               _coordinateMapper.MapSkeletonPointToColorPoint(new SkeletonPoint()
               {
                   X = cameraPoint.X,
                   Y = cameraPoint.Y,
                   Z = cameraPoint.Z
               }, _kinect360._colorManager == null ? ColorImageFormat.RgbResolution640x480Fps30 : _kinect360._colorManager.ColorImageFormat);
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