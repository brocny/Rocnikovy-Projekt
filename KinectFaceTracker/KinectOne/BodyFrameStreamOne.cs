using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.Kinect;
using Core.Kinect;
using FrameEdges = Core.Kinect.FrameEdges;
using MyJointType = Core.Kinect.JointType;
using KJointType = Microsoft.Kinect.JointType;
using Vector4 = System.Numerics.Vector4;

namespace KinectOne
{
    public class BodyFrameStreamOne : IBodyFrameStream
    {
        private readonly KinectOne _kinectOne;

        private BodyFrameReader _bodyFrameReader;
        private readonly BodyFrameSource _bodyFrameSource;

        public IBodyFrame GetNextFrame()
        {
            return new BodyFrame(_bodyFrameReader.AcquireLatestFrame());
        }

        public int BodyCount => _bodyFrameSource.BodyCount;

        public BodyFrameStreamOne(KinectOne kinectOne)
        {
            _kinectOne = kinectOne;
            _bodyFrameSource = _kinectOne.KinectSensor.BodyFrameSource;
        }

        private bool _isEventRegistered = false;
        private EventHandler<BodyFrameReadyEventArgs> _bodyFrameReady;
        private readonly object _eventLock = new object();
        public event EventHandler<BodyFrameReadyEventArgs> BodyFrameReady
        {
            add
            {
                lock (_eventLock)
                {
                    if (!_isEventRegistered)
                    {
                        _bodyFrameReader.FrameArrived += BodyFrameReader_FrameArrived;
                        _isEventRegistered = true;
                    }
                }
                _bodyFrameReady += value;
            }
            remove
            {
                _bodyFrameReady -= value;
                lock (_eventLock)
                {
                    if (_bodyFrameReady == null && _isEventRegistered)
                    {
                        _bodyFrameReader.FrameArrived -= BodyFrameReader_FrameArrived;
                        _isEventRegistered = false;
                    }
                }
            }
        }

        private void BodyFrameReader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            var bodyFrame = e.FrameReference.AcquireFrame();
            if (bodyFrame != null)
            {
                _bodyFrameReady?.Invoke(this, new BodyFrameReadyEventArgs(new BodyFrame(bodyFrame)));
            }
        }

        public void Open()
        {
            lock (_eventLock)
            {
                if (_bodyFrameReader == null)
                {
                    _bodyFrameReader = _bodyFrameSource.OpenReader();

                    if (!_isEventRegistered && _bodyFrameReady != null)
                    {
                        _bodyFrameReader.FrameArrived += BodyFrameReader_FrameArrived;
                    }

                }
                else
                {
                    _bodyFrameReader.IsPaused = false;
                }
            }
        }

        public void Close()
        {
            lock (_eventLock)
            {
                if (_bodyFrameReader != null)
                {
                    _bodyFrameReader.IsPaused = true;
                }
            }
        }

        public class BodyFrame : IBodyFrame
        {
            private readonly Microsoft.Kinect.BodyFrame _bodyFrame;

            public BodyFrame(Microsoft.Kinect.BodyFrame bodyFrame)
            {
                _bodyFrame = bodyFrame;
            }

            public int BodyCount => _bodyFrame.BodyCount;

            public Vector4 FloorClipPlane
            {
                get
                {
                    var fcp = _bodyFrame.FloorClipPlane;
                    return new Vector4(fcp.X, fcp.Y, fcp.Y, fcp.W);
                }
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

            public void Dispose()
            {
                _bodyFrame?.Dispose();
            }
        }

        public class BodyOne : IBody
        {
            private readonly Body _body;
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

                //Right hand
                new ValueTuple<MyJointType, MyJointType>(MyJointType.HandRight, MyJointType.HandTipRight),
                new ValueTuple<MyJointType, MyJointType>(MyJointType.HandRight, MyJointType.ThumbRight)
            };

            public IReadOnlyList<(MyJointType joint1, MyJointType joint2)> Bones => bones;

            public BodyOne(Body body)
            {
                _body = body;
            }

            public IReadOnlyDictionary<MyJointType, IJoint> Joints => _joints ??
                (_joints = _body.Joints.ToDictionary(x => x.Key.ToMyJointType(), x => (IJoint) new JointOne(x.Value)));
            private Dictionary<MyJointType, IJoint> _joints;

            public bool IsTracked => _body.IsTracked;
            public long TrackingId => (long) _body.TrackingId;
            public FrameEdges ClippedEdges => (FrameEdges) _body.ClippedEdges;
        }

        public class JointOne : IJoint
        {
            private readonly Joint _joint;
            
            public JointOne(Joint joint)
            {
                _joint = joint;
            }

            public Vector3 Position => new Vector3(_joint.Position.X, _joint.Position.Y, _joint.Position.Z);
            public bool IsTracked => _joint.TrackingState == TrackingState.Tracked;
            public MyJointType JointType => _joint.JointType.ToMyJointType();
        }
    }

    public static class JointTypeExtensions
    {
        public static KJointType ToKJointType(this MyJointType myJointType)
        {
            return (KJointType) (int) myJointType;
        }

        public static MyJointType ToMyJointType(this KJointType kJointType)
        {
            return (MyJointType) (int) kJointType;
        }
    }
}