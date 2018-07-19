using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Core;
using Core.Face;
using Core.Kinect;
using FsdkFaceLib;
using FsdkFaceLib.Properties;

namespace App.Main
{
    public partial class FormMain : Form
    {
        private readonly ICoordinateMapper _coordinateMapper;

        private FpsCounter _fpsCounter = new FpsCounter();

        private readonly DatabaseHelper<byte[]> _databaseHelper;
        private readonly KinectFaceTracker _kft;
        private readonly int _kinectFrameHeight;
        private readonly int _kinectFrameWidth;

        private readonly Renderer _renderer;

        private long _focusedFaceTrackingId;
        private int _imageHeight;
        private int _imageWidth;

        private Rectangle[] _lastFaceRects;
        private long[] _lastTrackingIds;

        private ProgramState _programState;

        public FormMain()
        {
            var kinect = KinectInitializeHelper.InitializeKinect();
            if (kinect == null)
            {
                Environment.Exit(1);
            }

            InitializeComponent();

            var faceDatabase = new DictionaryFaceDatabase<byte[]>(new FSDKFaceInfo());
            _databaseHelper = new DatabaseHelper<byte[]>(faceDatabase);

            var cts = new CancellationTokenSource();
            var facePipeline = new FSDKFacePipeline(faceDatabase, TaskScheduler.Default, cts.Token);

            if(KinectTrackedAppSettings.Default.RenderFace)
                facePipeline.FaceCuttingComplete += FacePipelineOnFaceCuttingComplete;

            if(FsdkSettings.Default.FsdkDetectFeatures || FsdkSettings.Default.FsdkDetectExpression)
                facePipeline.FacialFeatureDetectionComplete += FacePipelineOnFeatureDetection;

            if (KinectTrackedAppSettings.Default.RenderMatch)
                facePipeline.TemplateProcessingComplete += FacePipelineOnTemplateProcessingComplete;

            FsdkInitializeHelper.InitializeLibrary();

            _kinectFrameWidth = kinect.ColorFrameStream.FrameHeight;
            _kinectFrameHeight = kinect.ColorFrameStream.FrameWidth;
            _coordinateMapper = kinect.CoordinateMapper;
            _kft = new KinectFaceTracker(facePipeline, kinect, cts);
            _kft.FrameArrived += KftOnFrameArrived;

            _kft.FacePipeline.FacialFeatureDetectionComplete += FacePipelineOnFeatureDetection;
            _kft.Start();

            _imageHeight = mainPictureBox.Height;
            _imageWidth = mainPictureBox.Width;

            startStopToolStripMenuItem.Text =
                _programState == ProgramState.Running ? "Stop (Space)" : "Start (Space)";

            _renderer = new Renderer(_kinectFrameHeight, _kinectFrameWidth);
        }

        private void FacePipelineOnTemplateProcessingComplete(object sender, IEnumerable<KeyValuePair<long, Match<byte[]>>> matches)
        {
            var focusedFaceMatch = matches.SingleOrDefault(x => x.Key == _focusedFaceTrackingId).Value;

            if (focusedFaceMatch == null)
            {
                return;
            }

            if (focusedFaceMatch.Snapshot?.FaceImageBuffer != null)
            {
                matchedFacePictureBox.InvokeIfRequired(pb => pb.Image = focusedFaceMatch.Snapshot.FaceImageBuffer.ToBitmap());
            }

            matchLabel.Text = $"{focusedFaceMatch.Similarity * 100 :F2}%";
        }

        private void KftOnFrameArrived(object sender, FrameArrivedEventArgs e)
        {
            var faceLocations = e.FaceLocationResult;

            var bitmapTask = Task.Run(() => faceLocations.ImageBuffer.ToBitmap());

            var task = Task.Run(async () =>
            {
                int numFaces = faceLocations.FaceRectangles.Length;
                var faceLabelTask = Task.Run(() =>
                {
                    var labels = new string[numFaces];
                    for (int i = 0; i < numFaces; i++)
                    {
                        if (_kft.TrackedFaces.TryGetValue(faceLocations.TrackingIds[i], out var trackingStatus))
                        {
                            labels[i] = _kft.FaceDatabase[trackingStatus.TopTrackedCandidate.FaceId]?.Name ??
                                        $"ID: {trackingStatus.TopTrackedCandidate.FaceId}";
                        }
                    }

                    return labels;
                });

                _renderer.Image = await bitmapTask;
                _renderer.DrawBodies(faceLocations.Bodies, _coordinateMapper);
                _renderer.DrawRectangles(faceLocations.FaceRectangles, faceLocations.TrackingIds);
                _renderer.DrawNames(await faceLabelTask, faceLocations.FaceRectangles.Select(r => new Point(r.Left, r.Bottom)).ToArray(), faceLocations.TrackingIds);
                return _renderer.Image;
            });

            _fpsCounter.NewFrame();
            _lastFaceRects = faceLocations.FaceRectangles;
            _lastTrackingIds = faceLocations.TrackingIds;
            statusLabel.Text = $"FPS: {_fpsCounter.CurrentFps:F2} (Mean {_fpsCounter.AverageFps:F2} Min {_fpsCounter.MinFps:F2} Max {_fpsCounter.MaxFps:F2}){Environment.NewLine}Frames: {_fpsCounter.TotalFrames}";
            mainPictureBox.Image = task.Result;
        }

        private void FacePipelineOnFeatureDetection(object sender, FSDKFaceImage[] fsdkFaceImages)
        {
            var fsdkFaceImage = fsdkFaceImages.FirstOrDefault(f => f.TrackingId == _focusedFaceTrackingId);
            if (fsdkFaceImage == null)
                return;
            
            var (gender, confidence) = fsdkFaceImage.GetGender();
            if (gender == Gender.Unknown) return;
            var expression = fsdkFaceImage.GetExpression();
            var labelBuilder = new StringBuilder();
            if (_kft.TrackedFaces.TryGetValue(_focusedFaceTrackingId, out var trackingStatus))
            {
                if (_kft.FaceDatabase.TryGetValue(trackingStatus.TopTrackedCandidate.FaceId, out var faceInfo)
                    && !string.IsNullOrEmpty(faceInfo.Name))
                {
                    labelBuilder.AppendLine(faceInfo.Name);
                }

                labelBuilder.Append("ID: ");
                labelBuilder.AppendLine(trackingStatus.TopTrackedCandidate.FaceId.ToString());
            }

            labelBuilder.AppendLine($"Age: {fsdkFaceImage.GetAge():F1}");
            labelBuilder.AppendLine($"Smile: {expression.smile * 100:F1}%");
            labelBuilder.AppendLine($"Eyes Open: {expression.eyesOpen * 100:F1}%");
            labelBuilder.AppendLine($"{(gender == Gender.Male ? "Male:" : "Female:")} {confidence * 100:F1}%");

            faceLabel.InvokeIfRequired(f => f.Text = labelBuilder.ToString());
        }

        private void FacePipelineOnFaceCuttingComplete(object sender, FaceCutout[] faceCutouts)
        {
            if (faceCutouts == null || faceCutouts.Length == 0) return;

            var fco = faceCutouts.SingleOrDefault(x => x.TrackingId == _focusedFaceTrackingId) ?? faceCutouts[0];
            focusedFacePictureBox.InvokeIfRequired(f => f.Image = fco.ImageBuffer.ToBitmap());
            if (fco.TrackingId != _focusedFaceTrackingId)
            {
                _focusedFaceTrackingId = fco.TrackingId;
                matchedFacePictureBox.Image = null;
                matchLabel.Text = "";
            }
        }

        private void TogglePaused()
        {
            if (_programState == ProgramState.Paused)
                UnPause();
            else if (_programState == ProgramState.Running) Pause();
        }

        private void Pause()
        {
            if (!_kft.IsRunning) return;
            _kft.Stop();
            statusLabel.Text = "STOPPED";
            _programState = ProgramState.Paused;
            startStopToolStripMenuItem.Text = "Stop (Space)";
        }

        private void UnPause()
        {
            if (_kft.IsRunning) return;
            _fpsCounter = new FpsCounter();
            _kft.Start();
            statusLabel.Text = "";
            _programState = ProgramState.Running;
            startStopToolStripMenuItem.Text = "Start (Space)";
        }

        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            Pause();
           _databaseHelper.SaveBeforeClose();
        }

        private void FormMain_KeyPress(object sender, KeyPressEventArgs e)
        {
            // space to start/pause
            if (e.KeyChar == ' ') TogglePaused();
        }

        private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var originalState = _programState;
            Pause();

            _databaseHelper.Open();

            if (originalState == ProgramState.Running) UnPause();
        }

        private void SaveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var originalState = _programState;
            Pause();

            _databaseHelper.SaveAs();

            if (originalState == ProgramState.Running) UnPause();
        }

        private void SaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var originalState = _programState;
            Pause();

            _databaseHelper.Save();

            if (originalState == ProgramState.Running) UnPause();
        }

        private void StartStopSpaceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TogglePaused();
        }

        private void MainPictureBox_MouseClick(object sender, MouseEventArgs e)
        {
            if (_lastFaceRects == null) return;
            var clickPos = e.Location.Rescale(_imageWidth, _imageHeight, _kinectFrameHeight, _kinectFrameWidth);
            long id = 0;
            bool foundId = false;
            for (int i = 0; i < _lastFaceRects.Length; i++)
            {
                if (_lastFaceRects[i].Contains(clickPos))
                {
                    id = _lastTrackingIds[i];
                    foundId = true;
                    break;
                }
            }

            if (!foundId) return;

            switch (e.Button)
            {
                case MouseButtons.Left:
                    NameFace(id);
                    break;
                case MouseButtons.Right:
                    _kft.FacePipeline.Capture(id, ModifierKeys.HasFlag(Keys.Control));
                    break;
                case MouseButtons.Middle:
                    _focusedFaceTrackingId = id;
                    break;
                default:
                    return;
            }
        }

        private async void NameFace(long trackingId)
        {
            var inputNameForm = new InputNameForm();
            if (inputNameForm.ShowDialog() != DialogResult.OK)
                return;

            if (!_kft.TrackedFaces.TryGetValue(trackingId, out var trackingStatus))
            {
                trackingStatus = await _kft.FacePipeline.Capture(trackingId, true);
            }

            if (!_kft.FaceDatabase.TryGetValue(trackingStatus.TopTrackedCandidate.FaceId, out var faceInfo) || faceInfo == null)
                return;

            faceInfo.Name = inputNameForm.UserName;
        }

        private void MainPictureBox_SizeChanged(object sender, EventArgs e)
        {
            _imageWidth = mainPictureBox.Width;
            _imageHeight = mainPictureBox.Height;
        }

        private void FocusedFacePictureBox_MouseClick(object sender, MouseEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButtons.Left:
                    NameFace(_focusedFaceTrackingId);
                    break;
                case MouseButtons.Right:
                    _kft.FacePipeline.Capture(_focusedFaceTrackingId, ModifierKeys.HasFlag(Keys.Control));
                    break;
            }
        }

        private void ClearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _kft.FaceDatabase.Clear();
        }
    }
}

internal enum ProgramState
{
    Running,
    Paused
}