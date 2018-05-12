﻿using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Core.Face;
using Core;
using Core.Kinect;
using FsdkFaceLib;
using KinectFaceTracker;

namespace App.KinectTracked
{
    public partial class FormMain : Form
    {
        private const string FileFilter = "Xml files|*.xml|All files|*.*";

        private static readonly string DefaultDbPath =
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\KFT\FaceDb";

        private readonly ICoordinateMapper _coordinateMapper;

        private readonly FpsCounter _fpsCounter = new FpsCounter();

        private readonly KinectFaceTracker.KinectFaceTracker _kft;
        private readonly int _kinectFrameHeight;
        private readonly int _kinectFrameWidth;

        private readonly Renderer _renderer;

        private string _dbSerializePath;

        private long _focusedFaceTrackingId;
        private int _imageHeight;
        private int _imageWidth;

        private Rectangle[] _lastFaceRects;
        private long[] _lastTrackingIds;

        private ProgramState _programState;

        public FormMain()
        {
            var kinect = InitializeKinect();
            if (kinect == null)
            {
                Environment.Exit(1);
            }

            InitializeComponent();

            var cts = new CancellationTokenSource();
            var facePipeline = new FSDKFacePipeline(new DictionaryFaceDatabase<byte[]>(new FSDKFaceInfo()), TaskScheduler.Default, cts.Token);

            facePipeline.FaceCuttingComplete += FacePipelineOnFaceCuttingComplete;
            facePipeline.FacialFeatureDetectionComplete += FacePipelineOnFeatureDetection;
            facePipeline.TemplateProcessingComplete += FacePipelineOnTemplateProcessingComplete;

            FSDKFacePipeline.InitializeLibrary();
            _kinectFrameWidth = kinect.ColorFrameStream.FrameHeight;
            _kinectFrameHeight = kinect.ColorFrameStream.FrameWidth;
            _coordinateMapper = kinect.CoordinateMapper;
            _kft = new KinectFaceTracker.KinectFaceTracker(facePipeline, kinect, cts);
            _kft.FrameArrived += KftOnFrameArrived;

            _kft.FacePipeline.FacialFeatureDetectionComplete += FacePipelineOnFeatureDetection;
            _kft.Start();

            _imageHeight = mainPictureBox.Height;
            _imageWidth = mainPictureBox.Width;

            startStopSpaceToolStripMenuItem.Text =
                _programState == ProgramState.Running ? "Stop (Space)" : "Start (Space)";

            _renderer = new Renderer(_kinectFrameHeight, _kinectFrameWidth);
        }

        private void FacePipelineOnTemplateProcessingComplete(object sender, Match<byte[]>[] matches)
        {
            var focusedFaceMatch = matches.SingleOrDefault(x => x.TrackingId == _focusedFaceTrackingId);
            if (focusedFaceMatch?.Snapshot?.FaceImageBuffer != null)
            {
                matchedFacePictureBox.InvokeIfRequired(pb => pb.Image = focusedFaceMatch.Snapshot.FaceImageBuffer.ToBitmap());
            }
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
                            labels[i] = _kft.FaceDatabase.GetName(trackingStatus.TopCandidate.FaceId) ??
                                        $"ID: {trackingStatus.TopCandidate.FaceId}";
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
            statusLabel.Text = $"{_fpsCounter.Fps:F2} FPS";
            mainPictureBox.Image = task.Result;
        }

        private IKinect InitializeKinect()
        {
            IKinect kinect = null;
            bool tryAgain = false;
            do
            {
                try
                {
                    kinect = KinectFactory.KinectFactory.GetKinect();
                    tryAgain = false;
                }
                catch (ApplicationException)
                {
                    var result = MessageBox.Show("Error: No Kinect device found!", "Kinect not found",
                        MessageBoxButtons.RetryCancel,
                        MessageBoxIcon.Error);
                    if (result == DialogResult.Retry) tryAgain = true;

                    if (result == DialogResult.Cancel) return null;
                }
            } while (tryAgain);

            return kinect;
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
                if (_kft.FaceDatabase.TryGetValue(trackingStatus.TopCandidate.FaceId, out var faceInfo)
                    && !string.IsNullOrEmpty(faceInfo.Name))
                {
                    labelBuilder.AppendLine(faceInfo.Name);
                }

                labelBuilder.Append("ID: ");
                labelBuilder.AppendLine(trackingStatus.TopCandidate.FaceId.ToString());
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
            startStopSpaceToolStripMenuItem.Text = "Stop (Space)";
        }

        private void UnPause()
        {
            if (_kft.IsRunning) return;
            _kft.Start();
            statusLabel.Text = "";
            _programState = ProgramState.Running;
            startStopSpaceToolStripMenuItem.Text = "Start (Space)";
        }

        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            _kft.Stop();

            if(string.IsNullOrWhiteSpace(_dbSerializePath))
                return;

            var dialogResult = MessageBox.Show("Do you wish to save database before exiting?", "Save database", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                using (var stream = File.OpenWrite(_dbSerializePath))
                {
                    _kft.FaceDatabase.Serialize(stream);
                }
            }
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

            var dialog = new OpenFileDialog
            {
                InitialDirectory = DefaultDbPath,
                DefaultExt = "xml",
                Filter = FileFilter,
                Title = "Select file containing saved face database"
            };

            var result = dialog.STAShowDialog();
            // make a backup of the current database in case something goes wrong
            var backup = _kft.FaceDatabase.Backup();
            if (result == DialogResult.OK)
                try
                {
                    using (var fs = dialog.OpenFile())
                    {
                        _kft.FaceDatabase.Deserialize(fs);
                    }
                }
                catch (Exception exc)
                {
                    MessageBox.Show(
                        $"Error: An error occured while loading the database from {dialog.FileName}: {Environment.NewLine}{exc}");
                    // something went wrong -> revert
                    _kft.FaceDatabase.Restore(backup);
                }

            _dbSerializePath = dialog.FileName;
            if (originalState == ProgramState.Running) UnPause();
        }

        private void SaveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var originalState = _programState;
            Pause();

            if (!Directory.Exists(DefaultDbPath)) Directory.CreateDirectory(DefaultDbPath);

            var dialog = new SaveFileDialog
            {
                InitialDirectory = DefaultDbPath,
                DefaultExt = "xml",
                Filter = FileFilter
            };

            var result = dialog.STAShowDialog();
            if (result == DialogResult.OK)
            {
                try
                {
                    using (var fs = dialog.OpenFile())
                    {
                        _kft.FaceDatabase.Serialize(fs);
                    }
                }
                catch (Exception exc)
                {
                    MessageBox.Show(
                        $"Error: An error occured while saving the database to {dialog.FileName}:{Environment.NewLine}{exc}");
                }

                _dbSerializePath = dialog.FileName;
            }

            if (originalState == ProgramState.Running) UnPause();
        }

        private void SaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_dbSerializePath))
            {
                SaveAsToolStripMenuItem_Click(sender, e);
                return;
            }

            var originalState = _programState;
            Pause();

            try
            {
                using (var fs = File.OpenWrite(_dbSerializePath))
                {
                    _kft.FaceDatabase.Serialize(fs);
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show(
                    $"Error: An error occured while saving the database to {_dbSerializePath}:{Environment.NewLine}{exc}");
            }

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
                    _kft.FacePipeline.Capture(id);
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

            if (!_kft.TrackedFaces.TryGetValue(trackingId, out var status))
            {
                status = await _kft.FacePipeline.Capture(trackingId);
            }

            if (!_kft.FaceDatabase.TryGetValue(status.TopCandidate.FaceId, out var faceInfo))
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
                    _kft.FacePipeline.Capture(_focusedFaceTrackingId);
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