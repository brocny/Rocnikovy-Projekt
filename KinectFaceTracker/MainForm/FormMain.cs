﻿using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using KinectUnifier;
using LuxandFaceLib;
using System.Threading.Tasks;
using Face;

namespace Kinect_Test
{
    public partial class FormMain : Form
    {
        private ProgramState _programState;

        private readonly Renderer _renderer;

        private readonly IKinect _kinect;
        private readonly ICoordinateMapper _coordinateMapper;

        private int _colorBytesPerPixel;
        private int _colorHeight;
        private int _colorWidth;
        private long _displayedFaceTrackingId;

        private FpsCounter _fpsCounter = new FpsCounter();

        private LuxandFacePipeline _facePipeline;
        private FaceDatabase<byte[]> _faceDatabase;

        private IMultiManager _multiManager;

        private TaskScheduler _synchContext;

        private readonly Brush[] _bodyBrushes =
        {
            Brushes.LimeGreen, Brushes.Blue, Brushes.Yellow, Brushes.Orange, Brushes.DeepPink,
            Brushes.Red
        };

        private Pen[] _bodyPens;

        public FormMain()
        {
            InitializeComponent();
            var tryAgain = false;
            do
            {
                try
                {
                    _kinect = KinectFactory.KinectFactory.GetKinect();
                    tryAgain = false;
                }
                catch (ApplicationException)
                {
                    var result = MessageBox.Show("Error: No Kinect device found!", "Kinect not found",
                        MessageBoxButtons.RetryCancel,
                        MessageBoxIcon.Error);
                    if (result == DialogResult.Retry)
                    {
                        tryAgain = true;
                    }

                    if (result == DialogResult.Cancel)
                    {
                        Environment.Exit(1);
                    }
                }
            } while (tryAgain);
            

            _synchContext = TaskScheduler.FromCurrentSynchronizationContext();

            _faceDatabase = new FaceDatabase<byte[]>(new LuxandFaceInfo());
            _facePipeline = new LuxandFacePipeline(_faceDatabase);
            _facePipeline.FaceCuttingComplete += FacePipelineOnFaceCuttingComplete;

            LuxandFacePipeline.InitializeLibrary();
            _facePipeline.FacialFeatureRecognitionComplete += FacePipelineOnFeatureRecognition;

            _facePipeline.Completion.ContinueWith(t =>
            {
                if (t.Result.IsFaulted)
                {
                    throw t.Exception;
                }
            }, _synchContext);

            _multiManager = _kinect.OpenMultiManager(MultiFrameTypes.Body | MultiFrameTypes.Color);
            _multiManager.MultiFrameArrived += MultiManagerOnMultiFrameArrived;

            InitializeColorComponents();

            startStopSpaceToolStripMenuItem.Text =
                _programState == ProgramState.Running ? "Stop (Space)" : "Start (Space)";

            _renderer = new Renderer(_colorWidth, _colorHeight);
            _bodyPens = _bodyBrushes.Select(br => new Pen(br, 2.5f)).ToArray();

            _coordinateMapper = _kinect.CoordinateMapper;
            _kinect.Open();
        }

        private void FacePipelineOnFeatureRecognition(object sender, FSDKFaceImage[] fsdkFaceImages)
        {
            var fsdkFaceImage = fsdkFaceImages.FirstOrDefault(f => f.TrackingId == _displayedFaceTrackingId);
            if (fsdkFaceImage != null && _facePipeline.TrackedFaces.TryGetValue(_displayedFaceTrackingId, out int faceId))
            {
                var (gender, confidence) = fsdkFaceImage.GetGender();
                if (gender == Gender.Unknown) return;
                var expression = fsdkFaceImage.GetExpression();
                var text = $"ID: {faceId}{Environment.NewLine}" +
                           $"Age: {fsdkFaceImage.GetAge() :F2}{Environment.NewLine}" +
                           $"Smile: {expression.smile * 100 :F2}%{Environment.NewLine}" +
                           $"Eyes Open: {expression.eyesOpen * 100 :F2}%{Environment.NewLine}" +
                           $"{(gender == Gender.Male ? "Male:" : "Female:")} {confidence * 100 :F2}%";
                faceLabel.InvokeIfRequired(f => f.Text = text);
            }
        }

        private void FacePipelineOnFaceCuttingComplete(object sender, FaceCutout[] faceCutouts)
        {
            if (faceCutouts != null && faceCutouts.Length != 0)
            {
                var fco = faceCutouts[0];
                facePictureBox.InvokeIfRequired(f => f.Image = fco.PixelBuffer.BytesToBitmap(fco.Width, fco.Height, fco.BytesPerPixel));
                
                //facePictureBox.Image = faceCutouts[0].Bitmap;
                _displayedFaceTrackingId = faceCutouts[0].TrackingId;
            }
        }

        private void MultiManagerOnMultiFrameArrived(object sender, MultiFrameReadyEventArgs e)
        {
            var multiFrame = e.MultiFrame;
            if (multiFrame == null) return;

            using (var colorFrame = multiFrame.ColorFrame)
            using (var bodyFrame = multiFrame.BodyFrame)
            {
                if (colorFrame == null || bodyFrame == null) return;
                var locTask =
                    _facePipeline.LocateFacesAsync(colorFrame, bodyFrame, _coordinateMapper);
                var createBitMapTask = locTask.ContinueWith(t => t.Result.ColorBuffer.BytesToBitmap(t.Result.ImageWidth, t.Result.ImageHeight, t.Result.ImageBytesPerPixel));
                var renderTask = createBitMapTask.ContinueWith(t =>
                {
                    _renderer.Image = t.Result;
                    var res = locTask.Result;
                    _renderer.DrawBodies(res.Bodies, _bodyBrushes, _bodyPens, _coordinateMapper);
                    _renderer.DrawRectangles(res.FaceRectangles, _bodyPens);
                    for (int i = 0; i < res.FaceRectangles.Length; i++)
                    {   
                        if(_facePipeline.TrackedFaces.TryGetValue(res.TrackingIds[i], out var faceId))
                        {
                            var rect = res.FaceRectangles[i];
                            _renderer.DrawName(faceId.ToString(), rect.Left, rect.Bottom, _bodyBrushes[i]);
                        }
                    }

                    return _renderer.Image;
                });

                _fpsCounter.NewFrame();
                statusLabel.Text = $"{_fpsCounter.Fps:F2} FPS";
                Application.DoEvents();
                pictureBox1.Image = renderTask.Result;
            }
        }

        void InitializeColorComponents()
        {
            _colorWidth = _kinect.ColorManager.HeightPixels;
            _colorHeight = _kinect.ColorManager.WidthPixels;
            _colorBytesPerPixel = _kinect.ColorManager.BytesPerPixel;
        }

        private void TogglePaused()
        {
            if (_programState == ProgramState.Paused)
            {
                UnPause();
                startStopSpaceToolStripMenuItem.Text = "Stop (Space)";
            }
            else if (_programState == ProgramState.Running)
            {
                Pause();
                startStopSpaceToolStripMenuItem.Text = "Start (Space)";
            }
        }

        private void Pause()
        {
            if (_kinect.IsRunning)
            {
                _kinect.Close();
                statusLabel.Text = "STOPPED";
                _programState = ProgramState.Paused;
            }
        }

        private void UnPause()
        {
            if (!_kinect.IsRunning)
            {
                _kinect.Open();
                statusLabel.Text = "";
                _programState = ProgramState.Running;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            _kinect.Close();
        }

        private void FormMain_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == ' ')
            {
                TogglePaused();
            }
        }

        private const string FileFilter = "Xml files| *.xml|All files|*.*";
        private static readonly string DefaultDbPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\KFT\FaceDb";
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
            var copy = _faceDatabase.Clone();
            if (result == DialogResult.OK)
            {
                try
                {
                    using (var fs = dialog.OpenFile())
                    {
                        _faceDatabase.Deserialize(fs);
                    }
                }
                catch (Exception exc)
                {
                    MessageBox.Show(
                        $"Error: An error occured while loading the database from {dialog.FileName}: {Environment.NewLine}{exc}");
                    // something went wrong -> revert
                    _faceDatabase = (FaceDatabase<byte[]>) copy;
                }
            }

            _faceDatabase.SerializePath = dialog.FileName;
            if (originalState == ProgramState.Running) UnPause();
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var originalState = _programState;
            Pause();

            if (!Directory.Exists(DefaultDbPath))
            {
                Directory.CreateDirectory(DefaultDbPath);
            }

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
                        _faceDatabase.Serialize(fs);
                    }
                }
                catch (Exception exc)
                {
                    MessageBox.Show(
                        $"Error: An error occured while saving the database to {dialog.FileName}: {Environment.NewLine}{exc}");
                }

                _faceDatabase.SerializePath = dialog.FileName;
            }

            if (originalState == ProgramState.Running) UnPause();
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_faceDatabase.SerializePath == null)
            {
                saveAsToolStripMenuItem_Click(sender, e);
                return;
            }

            var originalState = _programState;
            Pause();

            try
            {
                using (var fs = File.OpenWrite(_faceDatabase.SerializePath))
                {
                    _faceDatabase.Serialize(fs);
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show(
                    $"Error: An error occured while saving the database to {_faceDatabase.SerializePath}: {Environment.NewLine}{exc}");
            }

            if(originalState == ProgramState.Running) UnPause();
        }

        private void startStopSpaceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TogglePaused();
        }
    }
}

enum ProgramState
{
    Running, Paused
}

class DialogState
{
    public DialogResult Result;
    public CommonDialog Dialog;

    public void ThreadProcShowDialog()
    {
        Result = Dialog.ShowDialog();
    }
}

static class DialogHelpers
{
    internal static DialogResult STAShowDialog(this CommonDialog dialog)
    {
        var state = new DialogState { Dialog = dialog };
        var thread = new
            System.Threading.Thread(state.ThreadProcShowDialog);
        thread.SetApartmentState(System.Threading.ApartmentState.STA);
        thread.Start();
        thread.Join();
        return state.Result;
    }
}

public static class ControlHelpers
{
    public static void InvokeIfRequired<T>(this T control, Action<T> action) where T : ISynchronizeInvoke
    {
        if (control.InvokeRequired)
        {
            control.Invoke(new Action(() => action(control)), null);
        }
        else
        {
            action(control);
        }
    }
}