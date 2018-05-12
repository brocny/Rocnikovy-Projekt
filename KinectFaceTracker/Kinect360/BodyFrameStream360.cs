﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.Kinect;
using Core.Kinect;

using Vector4 = System.Numerics.Vector4;
using MyJointType = Core.Kinect.JointType;
using KJointType = Microsoft.Kinect.JointType;


namespace Kinect360
{
    public class BodyFrameStream360 : IBodyFrameStream
    {
        private readonly Kinect360 _kinect360; 
        private readonly SkeletonStream _skeletonStream;
        public IBodyFrame GetNextFrame()
        {
            return new BodyFrame360(_skeletonStream.OpenNextFrame(30));
        }

        public int BodyCount => _skeletonStream.FrameSkeletonArrayLength;

        public BodyFrameStream360(Kinect360 kinect360)
        {
            _kinect360 = kinect360;
            _skeletonStream = _kinect360.KinectSensor.SkeletonStream;
        }

        private bool _isEventRegistered = false;
        private EventHandler<BodyFrameReadyEventArgs> _bodyFrameReady;
        public event EventHandler<BodyFrameReadyEventArgs> BodyFrameReady
        {
            add
            {
                if (!_isEventRegistered)
                {
                    _kinect360.KinectSensor.SkeletonFrameReady += KinectSensor_SkeletonFrameReady;
                    _isEventRegistered = true;
                }

                _bodyFrameReady += value;
            }
            remove
            {
                _bodyFrameReady -= value;
                if (_bodyFrameReady == null && _isEventRegistered)
                {
                    _kinect360.KinectSensor.SkeletonFrameReady -= KinectSensor_SkeletonFrameReady;
                    _isEventRegistered = false;
                }
            }
        }

        private void KinectSensor_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            var skeletonFrame = e.OpenSkeletonFrame();
            if (skeletonFrame != null)
            {
                _bodyFrameReady?.Invoke(this, new BodyFrameReadyEventArgs(new BodyFrame360(skeletonFrame)));
            }
        }

        public void Open()
        {
            _kinect360.KinectSensor.SkeletonStream.Enable();
            if (_bodyFrameReady != null && !_isEventRegistered)
            {
                _kinect360.KinectSensor.SkeletonFrameReady += KinectSensor_SkeletonFrameReady;
                _isEventRegistered = true;
            }
        }

        public void Close()
        {
            _kinect360.KinectSensor.SkeletonStream.Disable();
            if (_isEventRegistered)
            {
                _kinect360.KinectSensor.SkeletonFrameReady -= KinectSensor_SkeletonFrameReady;
                _isEventRegistered = false;
            }
        }

        public bool IsOpen => _skeletonStream.IsEnabled;

        public class BodyFrame360 : IBodyFrame
        {
            private readonly SkeletonFrame _skeletonFrame;

            public BodyFrame360(SkeletonFrame skeletonFrame)
            {
                _skeletonFrame = skeletonFrame ?? throw new ArgumentNullException(nameof(skeletonFrame));
            }

            public int BodyCount => _skeletonFrame.SkeletonArrayLength;

            public Vector4 FloorClipPlane
            {
                get
                {
                    var fcp = _skeletonFrame.FloorClipPlane;
                    return new Vector4(fcp.Item1, fcp.Item1, fcp.Item3, fcp.Item4);
                }
            }

            public void CopyBodiesTo(IBody[] bodies)
            {
                var skeletonData = new Skeleton[_skeletonFrame.SkeletonArrayLength];
                _skeletonFrame.CopySkeletonDataTo(skeletonData);
                int i = -1;
                int j = 0;
                while (++i < skeletonData.Length && j < bodies.Length)
                {
                    if(skeletonData[i] == null)
                        continue;

                    bodies[j++] = new Body360(skeletonData[i]);
                }
            }

            #region IDisposable Support
            private bool _disposedValue = false; // To detect redundant calls

            

            protected virtual void Dispose(bool disposing)
            {
                if (!_disposedValue)
                {
                    if (disposing)
                    {
                        _skeletonFrame?.Dispose();
                    }
                    
                    _disposedValue = true;
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
            public IReadOnlyDictionary<MyJointType, IJoint> Joints => _joints ?? (_joints = MakeJointDictionary());
            public IReadOnlyList<(MyJointType joint1, MyJointType joint2)> Bones => bones;
            public bool IsTracked => _body.TrackingState == SkeletonTrackingState.Tracked;
            public long TrackingId => _body.TrackingId;

            public Body360(Skeleton body)
            {
                _body = body ?? throw new ArgumentNullException(nameof(body));
            }
            
            private IReadOnlyDictionary<MyJointType, IJoint> _joints;

            private IReadOnlyDictionary<MyJointType, IJoint> MakeJointDictionary()
            {
                var dict = _body.Joints.ToDictionary(x => x.JointType.ToMyJointType(), x => (IJoint) new Joint360(x));
                var headPos = dict[MyJointType.Head].Position;
                var shoulderCenterPos = dict[MyJointType.ShoulderCenter].Position;
                var neckPos = headPos * 0.35f + shoulderCenterPos * 0.65f;
                dict[MyJointType.Neck] = new FakeJoint(neckPos, dict[MyJointType.Head].IsTracked && dict[MyJointType.ShoulderCenter].IsTracked, MyJointType.Neck);
                return dict;
            }

            private readonly Skeleton _body;

            private static readonly List<ValueTuple<MyJointType, MyJointType>> bones = new List<ValueTuple<MyJointType, MyJointType>>
            {
                // Torso
                new ValueTuple<MyJointType, MyJointType>(MyJointType.Head, MyJointType.Neck),
                new ValueTuple<MyJointType, MyJointType>(MyJointType.Neck, MyJointType.ShoulderCenter),
                new ValueTuple<MyJointType, MyJointType>(MyJointType.ShoulderCenter, MyJointType.SpineMid),
                new ValueTuple<MyJointType, MyJointType>(MyJointType.SpineMid, MyJointType.HipCenter),
                new ValueTuple<MyJointType, MyJointType>(MyJointType.ShoulderCenter, MyJointType.ShoulderRight),
                new ValueTuple<MyJointType, MyJointType>(MyJointType.ShoulderCenter, MyJointType.ShoulderLeft),
                new ValueTuple<MyJointType, MyJointType>(MyJointType.HipCenter, MyJointType.HipRight),
                new ValueTuple<MyJointType, MyJointType>(MyJointType.HipCenter, MyJointType.HipLeft),

                // Right Arm
                new ValueTuple<MyJointType, MyJointType>(MyJointType.ShoulderRight, MyJointType.ElbowRight),
                new ValueTuple<MyJointType, MyJointType>(MyJointType.ElbowRight, MyJointType.WristRight),
                new ValueTuple<MyJointType, MyJointType>(MyJointType.WristRight, MyJointType.HandRight),

                // Left Arm
                new ValueTuple<MyJointType, MyJointType>(MyJointType.ShoulderLeft, MyJointType.ElbowLeft),
                new ValueTuple<MyJointType, MyJointType>(MyJointType.ElbowLeft, MyJointType.WristLeft),
                new ValueTuple<MyJointType, MyJointType>(MyJointType.WristLeft, MyJointType.HandLeft),

                // Right Leg
                new ValueTuple<MyJointType, MyJointType>(MyJointType.HipRight, MyJointType.KneeRight),
                new ValueTuple<MyJointType, MyJointType>(MyJointType.KneeRight, MyJointType.AnkleRight),
                new ValueTuple<MyJointType, MyJointType>(MyJointType.AnkleRight, MyJointType.FootRight),

                // Left Leg
                new ValueTuple<MyJointType, MyJointType>(MyJointType.HipLeft, MyJointType.KneeLeft),
                new ValueTuple<MyJointType, MyJointType>(MyJointType.KneeLeft, MyJointType.AnkleLeft),
                new ValueTuple<MyJointType, MyJointType>(MyJointType.AnkleLeft, MyJointType.FootLeft),

                // Left hand 
                new ValueTuple<MyJointType, MyJointType>(MyJointType.HandLeft, MyJointType.HandTipLeft),
                new ValueTuple<MyJointType, MyJointType>(MyJointType.HandLeft, MyJointType.ThumbLeft),

                // Right hand
                new ValueTuple<MyJointType, MyJointType>(MyJointType.HandRight, MyJointType.HandTipRight),
                new ValueTuple<MyJointType, MyJointType>(MyJointType.HandRight, MyJointType.ThumbRight)
            };
        }

        public class FakeJoint : IJoint
        {
            public FakeJoint(Vector3 position, bool isTracked, MyJointType jointType)
            {
                Position = position;
                IsTracked = isTracked;
                JointType = jointType;
            }

            public Vector3 Position { get; }
            public bool IsTracked { get; }
            public MyJointType JointType { get; }
        }

        public class Joint360 : IJoint
        {
            private readonly Joint _joint;

            public Joint360(Joint joint)
            {
                _joint = joint;
            }

            public bool IsTracked => _joint.TrackingState == JointTrackingState.Tracked;
            public Vector3 Position => new Vector3(_joint.Position.X, _joint.Position.Y, _joint.Position.Z);
            public MyJointType JointType => _joint.JointType.ToMyJointType();
        }
    }

    internal static class JointTypeExtensions
    {
        public static MyJointType ToMyJointType(this KJointType kJointType)
        {
            return JointConversionTable[(int) kJointType];
        }

        private static readonly MyJointType[] JointConversionTable =
        {
            // 0 : Spine + head
            MyJointType.HipCenter, MyJointType.SpineMid, MyJointType.ShoulderCenter, MyJointType.Head,
            // 4 : Left arm
            MyJointType.ShoulderLeft, MyJointType.ElbowLeft, MyJointType.WristLeft, MyJointType.HandLeft,
            // 8 : Right arm
            MyJointType.ShoulderRight, MyJointType.ElbowRight, MyJointType.WristRight, MyJointType.HandRight,
            // 12 : Left leg
            MyJointType.HipLeft, MyJointType.KneeLeft, MyJointType.AnkleLeft, MyJointType.FootLeft,
            // 16 : Right leg
            MyJointType.HipRight, MyJointType.KneeRight, MyJointType.AnkleRight, MyJointType.FootRight,
        };
    }
}