﻿using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Face;
using KinectUnifier;
using LuxandFace;
using LuxandFaceLib;
using Microsoft.VisualBasic;

namespace KinectFaceTracker
{
    public partial class FormMain : Form
    {
        private const string FileFilter = "Xml files| *.xml|All files|*.*";

        private static readonly string DefaultDbPath =
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\KFT\FaceDb";

        private readonly Brush[] _bodyBrushes =
        {
            Brushes.LimeGreen, Brushes.Blue, Brushes.Yellow, Brushes.Orange, Brushes.DeepPink,
            Brushes.Red
        };


        private readonly Renderer _renderer;

        private readonly Pen[] _bodyPens;

        private readonly int _kinectFrameWidth;
        private readonly int _kinectFrameHeight;

        private long _displayedFaceTrackingId;

        private readonly FpsCounter _fpsCounter = new FpsCounter();
        private int _imageHeight;
        private int _imageWidth;

        private string _dbSerializePath;

        private readonly KinectFaceTracker _kft;

        private Rectangle[] _lastFaceRects;
        private long[] _lastTrackingIds;

        private ProgramState _programState;
        private readonly ICoordinateMapper _coordinateMapper;

        private readonly TaskScheduler _synchContext;

        public FormMain()
        {
            var kinect = InitializeKinect();
            if (kinect == null)
            {
                Environment.Exit(1);
            }
            InitializeComponent();

            _synchContext = TaskScheduler.FromCurrentSynchronizationContext();

            var facePipeline = new LuxandFacePipeline(new DictionaryFaceDatabase<byte[]>(new LuxandFaceInfo()));
            facePipeline.FaceCuttingComplete += FacePipelineOnFaceCuttingComplete;
            facePipeline.FacialFeatureDetectionComplete += FacePipelineOnFeatureDetection;

            LuxandFacePipeline.InitializeLibrary();
            _kinectFrameWidth = kinect.ColorManager.HeightPixels;
            _kinectFrameHeight = kinect.ColorManager.WidthPixels;
            _coordinateMapper = kinect.CoordinateMapper;
            _kft = new KinectFaceTracker(facePipeline, kinect);
            _kft.FrameArrived += KftOnFrameArrived;

            _kft.FacePipeline.FacialFeatureDetectionComplete += FacePipelineOnFeatureDetection;
            _kft.Start();

            _imageHeight = pictureBox1.Height;
            _imageWidth = pictureBox1.Width;

            startStopSpaceToolStripMenuItem.Text =
                _programState == ProgramState.Running ? "Stop (Space)" : "Start (Space)";

            _renderer = new Renderer(_kinectFrameHeight, _kinectFrameWidth);
            _bodyPens = _bodyBrushes.Select(br => new Pen(br, 2.5f)).ToArray();
            
        }
        
        private void KftOnFrameArrived(object sender, FrameArrivedEventArgs e)
        {
            var faceLocations = e.FaceLocationResult;

            var bitmapTask = Task.Run(() => faceLocations.ColorBuffer.BytesToBitmap(faceLocations.ImageWidth, faceLocations.ImageHeight,
                faceLocations.ImageBytesPerPixel));

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
                                               trackingStatus.TopCandidate.FaceId.ToString();
                        }
                    }

                    return labels;
                });

                _renderer.Image = await bitmapTask;
                _renderer.DrawBodies(faceLocations.Bodies, _bodyBrushes, _bodyPens, _coordinateMapper);
                _renderer.DrawRectangles(faceLocations.FaceRectangles, _bodyPens);

                var faceLabels = await faceLabelTask;
                for (int i = 0; i < numFaces; i++)
                {
                    var rect = faceLocations.FaceRectangles[i];
                    _renderer.DrawName(faceLabels[i], rect.Left, rect.Bottom, _bodyBrushes[i]);
                }
                return _renderer.Image;
            });

            _fpsCounter.NewFrame();
            _lastFaceRects = faceLocations.FaceRectangles;
            _lastTrackingIds = faceLocations.TrackingIds;
            statusLabel.Text = $"{_fpsCounter.Fps:F2} FPS";
            pictureBox1.Image = task.Result;
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
            var fsdkFaceImage = fsdkFaceImages.FirstOrDefault(f => f.TrackingId == _displayedFaceTrackingId);
            if (fsdkFaceImage != null)
            {
                var (gender, confidence) = fsdkFaceImage.GetGender();
                if (gender == Gender.Unknown) return;
                var expression = fsdkFaceImage.GetExpression();
                StringBuilder labelBuilder = new StringBuilder();
                if (_kft.TrackedFaces.TryGetValue(_displayedFaceTrackingId, out var trackingStatus))
                {
                    if (_kft.FaceDatabase.TryGetValue(trackingStatus.TopCandidate.FaceId, out var faceInfo)
                        && !string.IsNullOrEmpty(faceInfo.Name))
                    {
                        labelBuilder.AppendLine(faceInfo.Name);
                    }

                    labelBuilder.Append("ID: ");
                    labelBuilder.AppendLine(trackingStatus.TopCandidate.FaceId.ToString());
                }

                labelBuilder.AppendLine($"Age: {fsdkFaceImage.GetAge():F2}");
                labelBuilder.AppendLine($"Smile: {expression.smile * 100:F2}%");
                labelBuilder.AppendLine($"Eyes Open: {expression.eyesOpen * 100:F2}%");
                labelBuilder.AppendLine($"{(gender == Gender.Male ? "Male:" : "Female:")} {confidence * 100:F2}%");
                        
                faceLabel.InvokeIfRequired(f => f.Text = labelBuilder.ToString());
            }
        }

        private void FacePipelineOnFaceCuttingComplete(object sender, FaceCutout[] faceCutouts)
        {
            if (faceCutouts != null && faceCutouts.Length != 0)
            {
                var fco = faceCutouts[0];
                facePictureBox.InvokeIfRequired(f =>
                    f.Image = fco.PixelBuffer.BytesToBitmap(fco.Width, fco.Height, fco.BytesPerPixel));
                _displayedFaceTrackingId = faceCutouts[0].TrackingId;
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
            if (_kft.IsRunning)
            {
                _kft.Stop();
                statusLabel.Text = "STOPPED";
                _programState = ProgramState.Paused;
                startStopSpaceToolStripMenuItem.Text = "Stop (Space)";
            }
        }

        private void UnPause()
        {
            if (!_kft.IsRunning)
            {
                _kft.Start();
                statusLabel.Text = "";
                _programState = ProgramState.Running;
                startStopSpaceToolStripMenuItem.Text = "Start (Space)";
            }
        }

        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            _kft.Stop();
        }

        private void FormMain_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == ' ') TogglePaused();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
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

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
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

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_dbSerializePath))
            {
                saveAsToolStripMenuItem_Click(sender, e);
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

        private void startStopSpaceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TogglePaused();
        }

        private void pictureBox1_MouseClick(object sender, MouseEventArgs e)
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

            if(!foundId) return;

            switch (e.Button)
            {
                case MouseButtons.Left:
                    NameFace(id);
                    break;
                case MouseButtons.Right:
                    CaptureFace(id);
                    break;
                default:
                    return;
            }
        }

        private Task<TrackingStatus> CaptureFace(long trackingId)
        {
            return _kft.FacePipeline.Capture(trackingId);
        }

        private async void NameFace(long trackingId)
        {
            string name = Interaction.InputBox("Enter person's name", "Person name dialog", null);
            if (string.IsNullOrWhiteSpace(name)) return;

            if (!_kft.TrackedFaces.TryGetValue(trackingId, out var status))
            {
                status = await CaptureFace(trackingId);
            }

            if (!_kft.FaceDatabase.TryGetValue(status.TopCandidate.FaceId, out var faceInfo))
                return;

            faceInfo.Name = name;
        }

        private void pictureBox1_SizeChanged(object sender, EventArgs e)
        {
            _imageWidth = pictureBox1.Width;
            _imageHeight = pictureBox1.Height;
        }

        private void facePictureBox_MouseClick(object sender, MouseEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButtons.Left:
                    NameFace(_displayedFaceTrackingId);
                    break;
                case MouseButtons.Right:
                    CaptureFace(_displayedFaceTrackingId);
                    break;
            }
        }
    }
}

internal enum ProgramState
{
    Running,
    Paused
}