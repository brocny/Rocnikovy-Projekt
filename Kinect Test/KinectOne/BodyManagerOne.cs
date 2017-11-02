using System;
using System.Collections.Generic;
using KinectUnifier;
using Microsoft.Kinect;
using JointType = KinectUnifier.JointType;

namespace KinectOne
{
    public class BodyManagerOne : IBodyManager
    {
        private KinectOne _kinectOne;

        private BodyFrameReader _bodyFrameReader;
        private BodyFrameSource _bodyFrameSource;

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
            private BodyFrame _bodyFrame;

            public BodyFrameOne(BodyFrame bodyFrame)
            {
                _bodyFrame = bodyFrame;
            }

            public int BodyCount => _bodyFrame.BodyCount;

            public Point4F FloorClipPlane
            {
                get
                {
                    var fcp = _bodyFrame.FloorClipPlane;
                    return new Point4F(fcp.X, fcp.Y, fcp.Y, fcp.W);
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
            static readonly List<ValueTuple<JointType, JointType>> bones = new List<ValueTuple<JointType, JointType>>
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

                //Right hand
                new ValueTuple<JointType, JointType>(JointType.HandRight, JointType.HandTipRight),
                new ValueTuple<JointType, JointType>(JointType.HandRight, JointType.ThumbRight)
            };

            public IReadOnlyList<ValueTuple<JointType, JointType>> Bones => bones;

            public BodyOne(Body body)
            {
                _body = body;
                _joints = new Dictionary<JointType, IJoint>(25);
                //TODO: Do this in O(1) instead of O(n)
                foreach (var joint in _body.Joints)
                {
                    _joints.Add((JointType)(int)joint.Key, new JointOne(joint.Value));
                }
            }

            
            public IReadOnlyDictionary<KinectUnifier.JointType, IJoint> Joints => _joints;
            private Dictionary<KinectUnifier.JointType, IJoint> _joints;

            public bool IsTracked => _body.IsTracked;
        }

        public class JointOne : IJoint
        {
            private Joint _joint;
            
            public JointOne(Joint joint)
            {
                _joint = joint;
            }

            public Point3F Position => new Point3F(_joint.Position.X, _joint.Position.Y, _joint.Position.Z);
            public bool IsTracked => _joint.TrackingState == TrackingState.Tracked;

        }

    }
}