using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.Kinect;
using KinectUnifier;

using MyJointType = KinectUnifier.JointType;
using KJointType = Microsoft.Kinect.JointType;
using Vector4 = System.Numerics.Vector4;

namespace KinectOne
{
    public class BodyManagerOne : IBodyManager
    {
        private readonly KinectOne _kinectOne;

        private BodyFrameReader _bodyFrameReader;
        private readonly BodyFrameSource _bodyFrameSource;

        public int BodyCount => _bodyFrameSource.BodyCount;

        public BodyManagerOne(KinectOne kinectOne)
        {
            _kinectOne = kinectOne;
            _bodyFrameSource = _kinectOne.KinectSensor.BodyFrameSource;
        }

        public event EventHandler<BodyFrameReadyEventArgs> BodyFrameReady;

        private void BodyFrameReader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            var bodyFrame = e.FrameReference.AcquireFrame();
            if (bodyFrame != null)
            {
                BodyFrameReady?.Invoke(this, new BodyFrameReadyEventArgs(new BodyFrameOne(bodyFrame)));
            }
        }

        public void Open()
        {
            _bodyFrameReader = _bodyFrameSource.OpenReader();
            _bodyFrameReader.FrameArrived += BodyFrameReader_FrameArrived;
        }

        public void Close()
        {
            _bodyFrameReader.Dispose();
        }

        public class BodyFrameOne : IBodyFrame
        {
            private readonly BodyFrame _bodyFrame;

            public BodyFrameOne(BodyFrame bodyFrame)
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

            public IReadOnlyList<ValueTuple<MyJointType, MyJointType>> Bones => bones;

            public BodyOne(Body body)
            {
                _body = body;;
            }

            public IReadOnlyDictionary<MyJointType, IJoint> Joints => _joints ??
                (_joints = _body.Joints.ToDictionary(x => x.Key.ToMyJointType(), x => (IJoint) new JointOne(x.Value)));
            private Dictionary<MyJointType, IJoint> _joints;

            public bool IsTracked => _body.IsTracked;
            public long TrackingId => (long)_body.TrackingId;
        }

        public class JointOne : IJoint
        {
            private Joint _joint;
            
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