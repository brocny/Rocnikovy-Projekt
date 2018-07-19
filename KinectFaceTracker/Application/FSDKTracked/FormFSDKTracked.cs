// Based on a sample included in the Luxand Face SDK, modified and adapted to use a Kinect instead of a webcam

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Core;
using Core.Kinect;
using FsdkFaceLib;
using Luxand;

namespace App.FSDKTracked
{
    public partial class FormFSDKTracked : Form
    {
        // program states: whether we recognize faces, or user has clicked a face
        private enum ProgramState { Running, Paused}

        private ProgramState _programState;

        private const int MaxUserNameLength = 128;

        private const string StartButtonStoppedText = "Start (Space)";
        private const string StartButtonRunningText = "Stop (Space)";

        private const string WaitingLabelText = "Waiting for Kinect...";
        private const string StoppedLabelText = "STOPPED";

        private IKinect _kinect;
        private IColorFrameStream _colorFrameStream;
        private byte[] _imageBuffer;
        private float _imageWidthRatio = 1f;
        private float _imageHeightRatio = 1f;

        private readonly List<(long id, Rectangle rect)> _lastFaceRects = new List<(long id, Rectangle rect)>();

        private readonly string _initialDirectory = ".\\FaceDB\\Tracker";

        private string _trackerMemoryFile = "";

        private int _trackerHandle;
        private Font _nameFont;
        private readonly StringFormat _nameFormat = new StringFormat { Alignment = StringAlignment.Center };
        private readonly Pen _limeGreenPen = new Pen(Color.LimeGreen, 2.5f);
        private readonly Pen _bluePen = new Pen(Color.Blue, 2.5f);

        private FpsCounter _fpsCounter = new FpsCounter();

        public FormFSDKTracked()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            FSDKFacePipeline.InitializeLibrary();
            _kinect = KinectInitializeHelper.InitializeKinect();

            _colorFrameStream = _kinect.ColorFrameStream;
            _colorFrameStream.Open(true);
            _imageBuffer = new byte[_colorFrameStream.FrameDataSize];
            _imageWidthRatio = _colorFrameStream.FrameWidth / (float)pictureBox1.Width;
            _imageHeightRatio = _colorFrameStream.FrameHeight / (float)pictureBox1.Height;
            _nameFont = new Font("Arial", 12f * _imageWidthRatio);
            _programState = ProgramState.Paused;
            statusLabel.Text = WaitingLabelText;


            FSDK.CreateTracker(ref _trackerHandle);
            SetTrackerParams();

            _colorFrameStream.ColorFrameReady += ColorFrameStreamOnColorFrameReady;

            Unpause();
        }
      

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(_trackerMemoryFile))
            {
                var result = MessageBox.Show("Do you want to save tracker memory?", "Save tracker memory?", MessageBoxButtons.YesNo);
                if (result == DialogResult.Yes)
                {
                    FSDK.SaveTrackerMemoryToFile(_trackerHandle, _trackerMemoryFile);
                }
            }
            
            FSDK.FreeTracker(_trackerHandle);
            Application.Exit();
        }

        private void ColorFrameStreamOnColorFrameReady(object sender, ColorFrameReadyEventArgs e)
        {
            const int maximumFacesDetected = 32;

            using (var frame = e.ColorFrame)
            {
                if (frame == null)
                {
                    Application.DoEvents();
                    return;
                }

                Array.Resize(ref _imageBuffer, frame.PixelDataLength);
                frame.CopyFramePixelDataToArray(_imageBuffer);
                var imageBuffer = new ImageBuffer(_imageBuffer, frame.Width, frame.Height, frame.BytesPerPixel);

                var bitmapTask = Task.Run(() => imageBuffer.ToBitmap());

                imageBuffer.CreateFsdkImageHandle(out int fsdkImageHandle);
                var fsdkImage = new FSDK.CImage(fsdkImageHandle);

                long faceCount = 0;
                FSDK.FeedFrame(_trackerHandle, 0, fsdkImage.ImageHandle, ref faceCount, out var ids,
                    sizeof(long) * maximumFacesDetected);
                Array.Resize(ref ids, (int) faceCount);


                var bitmap = bitmapTask.Result;
                var graphics = Graphics.FromImage(bitmap);
                _lastFaceRects.Clear();

                foreach (long id in ids)
                {
                    var facePosition = new FSDK.TFacePosition();
                    FSDK.GetTrackerFacePosition(_trackerHandle, 0, id, ref facePosition);

                    int left = facePosition.xc - (int) (facePosition.w * 0.6);
                    int top = facePosition.yc - (int) (facePosition.w * 0.5);
                    int w = (int) (facePosition.w * 1.2);

                    var rect = new Rectangle(left, top, w ,w);
                    _lastFaceRects.Add((id, rect));
                    

                    int res = FSDK.GetName(_trackerHandle, id, out string name, MaxUserNameLength);

                    if (FSDK.FSDKE_OK == res && name.Length > 0)
                    {
                        graphics.DrawString(name, _nameFont,
                            Brushes.LimeGreen,
                            facePosition.xc, top + w + 5, _nameFormat);
                    }

                    var pen = _limeGreenPen;

                    graphics.DrawRectangle(pen, left, top, w, w);
                }

                pictureBox1.Image = bitmap;
                _fpsCounter.NewFrame();
                statusLabel.Text = $"FPS: {_fpsCounter.CurrentFps:F2} (Mean {_fpsCounter.AverageFps:F2} Min {_fpsCounter.MinFps:F2} Max {_fpsCounter.MaxFps:F2}){Environment.NewLine}Frames: {_fpsCounter.TotalFrames}";

                Application.DoEvents();
            }
        }
    
    

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            var imageX = (int)(e.Location.X * _imageWidthRatio);
            var imageY = (int)(e.Location.Y * _imageHeightRatio);

            foreach ((long id, Rectangle rect) in _lastFaceRects)
            {
                if (rect.Contains(imageX, imageY))
                {
                    if (FSDK.FSDKE_OK == FSDK.LockID(_trackerHandle, id))
                    {

                        if (e.Button == MouseButtons.Left)
                        {
                            // get the user name
                            var inputNameForm = new InputNameForm();

                            if (DialogResult.OK == inputNameForm.ShowDialog())
                            {
                                var userName = inputNameForm.UserName;
                                if (string.IsNullOrWhiteSpace(userName))
                                {
                                    FSDK.SetName(_trackerHandle, id, id.ToString());
                                    FSDK.PurgeID(_trackerHandle, id);
                                }
                                else
                                {
                                    if (userName.Length >= MaxUserNameLength)
                                    {
                                        userName = userName.Substring(0, 256);
                                    }

                                    FSDK.SetName(_trackerHandle, id, userName);
                                }
                            }
                        }
                        else if(e.Button == MouseButtons.Right)
                        {
                            FSDK.SetName(_trackerHandle, id, id.ToString());
                        }

                        FSDK.UnlockID(_trackerHandle, id);
                    }
                }
            }
        }

        private void Pause()
        {
            if (_programState == ProgramState.Paused) return;

            _kinect.Close();
            _programState = ProgramState.Paused;
            startStopToolStripMenuItem.Text = StartButtonStoppedText;
            statusLabel.Text = StoppedLabelText;
        }

        private void Unpause()
        {
            if(_programState != ProgramState.Paused) return;

            _fpsCounter = new FpsCounter();
            _programState = ProgramState.Running;
            _kinect.Open();
            statusLabel.Text = WaitingLabelText;
            startStopToolStripMenuItem.Text = StartButtonRunningText;
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                InitialDirectory = _initialDirectory,
                DefaultExt = "dat",
                Filter = FSDKTrackedAppSettings.Default.FileFilter,
                Title = "Select file containing saved face database"
            };

            if (DialogResult.OK == dialog.STAShowDialog())
            {
                FSDK.LoadTrackerMemoryFromFile(ref _trackerHandle, dialog.FileName);
            }

            SetTrackerParams();
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                InitialDirectory = _initialDirectory,
                DefaultExt = "dat",
                Filter = FSDKTrackedAppSettings.Default.FileFilter,
                Title = "Save as..."
            };

            if (DialogResult.OK == dialog.STAShowDialog())
            {
                FSDK.SaveTrackerMemoryToFile(_trackerHandle, dialog.FileName);
                _trackerMemoryFile = dialog.FileName;
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(_trackerMemoryFile))
            {
                FSDK.SaveTrackerMemoryToFile(_trackerHandle, _trackerMemoryFile);
            }
            else
            {
                saveAsToolStripMenuItem_Click(sender, e);
            }
        }

        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _trackerMemoryFile = "";
            FSDK.ClearTracker(_trackerHandle);
            var err = 0;
            SetTrackerParams();
        }

        private void SetTrackerParams()
        {
            int err = 0;
            // ToString().ToLowerInvariant() needed because false.ToString() == "False", but FSDK expects "false"
            var parameters =
                $"HandleArbitraryRotations={FSDKTrackedAppSettings.Default.FsdkHandleArbitraryRot.ToString().ToLowerInvariant()}; " +
                $"DetermineFaceRotationAngle={FSDKTrackedAppSettings.Default.FsdkDetermineRotAngle.ToString().ToLowerInvariant()}; " +
                $"InternalResizeWidth={FSDKTrackedAppSettings.Default.FsdkInternalResizeWidth}; " +
                $"FaceDetectionThreshold={FSDKTrackedAppSettings.Default.FsdkFaceDetectionThreshold};" +
                $"Learning={FSDKTrackedAppSettings.Default.EnableLearning.ToString().ToLowerInvariant()};" +
                $"Threshold={FSDKTrackedAppSettings.Default.MatchThreshold.ToString(CultureInfo.InvariantCulture)};" +
                $"DetectFacialFeatures={FSDKTrackedAppSettings.Default.DetectFeatures.ToString().ToLowerInvariant()};" +
                $"DetectAge={FSDKTrackedAppSettings.Default.DetectAge.ToString().ToLowerInvariant()};" +
                $"DetectGender={FSDKTrackedAppSettings.Default.DetectGender.ToString().ToLowerInvariant()};" +
                $"DetectExperssion={FSDKTrackedAppSettings.Default.DetectExpression.ToString().ToLowerInvariant()};";

            FSDK.SetTrackerMultipleParameters(_trackerHandle,
                parameters,
                ref err);

            if(err != 0) throw new ApplicationException($"An error has occured when setting tracker paramters at characters \"{parameters.Substring(Math.Max(0, err -10), 20)}\"");
        }
        

        private void startStopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_programState == ProgramState.Running)
            {
                Pause();
                return;
            }

            if (_programState == ProgramState.Paused)
            {
                Unpause();
            }
        }

        private void FormFSDKTracked_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == ' ')
            {
                startStopToolStripMenuItem_Click(sender, EventArgs.Empty);
            }
        }
    }
}
