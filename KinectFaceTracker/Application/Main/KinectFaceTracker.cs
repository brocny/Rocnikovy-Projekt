﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Core.Face;
using Core.Kinect;
using FsdkFaceLib;

namespace App.Main
{
    public class KinectFaceTracker : IDisposable
    {
        private readonly ICoordinateMapper _coordinateMapper;

        private readonly IKinect _kinect;
        private IMultiFrameStream _multiFrameStream;
        private CancellationTokenSource _cancellationTokenSource;

        public KinectFaceTracker(FSDKFacePipeline facePipeline, IKinect kinect, CancellationTokenSource cts)
        {
            _kinect = kinect;
            FacePipeline = facePipeline;
            _cancellationTokenSource = cts;
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
            _multiFrameStream.Dispose();
        }

        public event EventHandler<FrameArrivedEventArgs> FrameArrived;

        public void Start()
        {
            if (_multiFrameStream == null)
            {
                _multiFrameStream = _kinect.OpenMultiManager(MultiFrameTypes.Body | MultiFrameTypes.Color, true);
                _multiFrameStream.MultiFrameArrived += MultiFrameStreamOnMultiFrameArrived;
            }

            if (_cancellationTokenSource != null && _cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource = new CancellationTokenSource();
            }

            FacePipeline.Resume(_cancellationTokenSource?.Token ?? CancellationToken.None);
            _kinect.Open();
        }

        public void Stop()
        {
            _kinect.Close();
            _cancellationTokenSource.Cancel();
        }

        private async void MultiFrameStreamOnMultiFrameArrived(object sender, MultiFrameReadyEventArgs e)
        {
            using (var multiFrame = e.MultiFrame)
            {
                if (multiFrame == null) return;

                //if (_facePipeline.Completion.IsCompleted) throw _facePipeline.Completion.Result.Exception.InnerException;

                var colorFrame = multiFrame.ColorFrame;
                var bodyFrame = multiFrame.BodyFrame;

                if (colorFrame == null || bodyFrame == null) return;
                try
                {
                    var faceLocations =
                        await FacePipeline.LocateFacesAsync(colorFrame, bodyFrame, _coordinateMapper);
                    FrameArrived?.Invoke(this, new FrameArrivedEventArgs(faceLocations));
                }
                catch (TaskCanceledException)
                {
                }
            }
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