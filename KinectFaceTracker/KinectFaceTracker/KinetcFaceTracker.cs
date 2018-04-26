using System;
using System.Collections.Generic;
using Face;
using KinectUnifier;
using LuxandFace;
using LuxandFaceLib;

namespace KinectFaceTracker
{
    public class KinectFaceTracker : IDisposable
    {
        private readonly ICoordinateMapper _coordinateMapper;

        private readonly IKinect _kinect;
        private IMultiManager _multiManager;

        public KinectFaceTracker(FSDKFacePipeline facePipeline, IKinect kinect)
        {
            _kinect = kinect;
            FacePipeline = facePipeline;
            _coordinateMapper = _kinect.CoordinateMapper;
        }

        public FSDKFacePipeline FacePipeline { get; }

        public IReadOnlyDictionary<long, TrackingStatus> TrackedFaces => FacePipeline.TrackedFaces;

        public IFaceDatabase<byte[]> FaceDatabase
        {
            get => FacePipeline.FaceDb;
            set => FacePipeline.FaceDb = value;
        }

        public bool IsRunning => _kinect.IsRunning;

        public void Dispose()
        {
            _multiManager.Dispose();
        }

        public event EventHandler<FrameArrivedEventArgs> FrameArrived;

        public void Start()
        {
            if (_multiManager == null)
            {
                _multiManager = _kinect.OpenMultiManager(MultiFrameTypes.Body | MultiFrameTypes.Color, true);
                _multiManager.MultiFrameArrived += MultiManagerOnMultiFrameArrived;
            }

            _kinect.Open();
        }

        public void Stop()
        {
            _kinect.Close();
        }

        private async void MultiManagerOnMultiFrameArrived(object sender, MultiFrameReadyEventArgs e)
        {
            var multiFrame = e.MultiFrame;
            if (multiFrame == null) return;

            //if (_facePipeline.Completion.IsCompleted) throw _facePipeline.Completion.Result.Exception.InnerException;

            using (var colorFrame = multiFrame.ColorFrame)
            using (var bodyFrame = multiFrame.BodyFrame)
            {
                if (colorFrame == null || bodyFrame == null) return;
                var faceLocations = await FacePipeline.LocateFacesAsync(colorFrame, bodyFrame, _coordinateMapper);
                FrameArrived?.Invoke(this, new FrameArrivedEventArgs(faceLocations));
            }

            multiFrame.Dispose();
        }
    }

    public class FrameArrivedEventArgs : EventArgs
    {
        public FrameArrivedEventArgs(FaceLocationResult flr)
        {
            FaceLocationResult = flr;
        }

        public FaceLocationResult FaceLocationResult { get; }
    }
}