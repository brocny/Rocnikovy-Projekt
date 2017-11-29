using System;
using System.Collections.Generic;
using KinectUnifier;
using Microsoft.Kinect;
using JointType = KinectUnifier.JointType;


namespace Kinect360
{
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
            public long TrackingId => _body.TrackingId;

            public Body360(Skeleton body)
            {
                _body = body;
                _joints = new Dictionary<KinectUnifier.JointType, IJoint>(20);

                //TODO: Do this in O(1) instead of O(n)
                for (int i = 0; i < 20; i++)
                {
                    _joints.Add(_jointNumbers[i], new Joint360(_body.Joints[(Microsoft.Kinect.JointType)i]));
                }
                
                var headPos = _joints[JointType.Head].Position;
                var shoulderCenterPos = _joints[JointType.ShoulderCenter].Position;
                var neckPos = headPos + shoulderCenterPos / 2;
                _joints.Add(
                    KinectUnifier.JointType.Neck, 
                    new FakeJoint(neckPos, _joints[KinectUnifier.JointType.Head].IsTracked && _joints[KinectUnifier.JointType.ShoulderCenter].IsTracked));
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

            private static readonly List<ValueTuple<JointType, JointType>> _bones = new List<ValueTuple<JointType, JointType>>
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

        public class FakeJoint : IJoint
        {
            public FakeJoint(Point3F position, bool isTracked)
            {
                Position = position;
                IsTracked = isTracked;
            }

            public Point3F Position { get; }
            public bool IsTracked { get; }
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


}