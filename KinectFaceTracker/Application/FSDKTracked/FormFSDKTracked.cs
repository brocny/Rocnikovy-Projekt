﻿// Based on a sample included in the Luxand Face SDK, modified and adapted to use a Kinect instead of a webcam

using System;
using System.Drawing;
using System.IO;
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
        private enum ProgramState { Remember, Name, Recognize, Stopped, Unitialized }

        private ProgramState _programState = ProgramState.Unitialized;

        private const string StartButtonStoppedText = "Start";
        private const string StartButtonRunningText = "Stop";

        private const string WaitingLabelText = "Waiting for Kinect...";
        private const string StoppedLabelText = "STOPPED";

        private IKinect _kinect;
        private IColorFrameStream _colorFrameStream;
        private byte[] _imageBuffer;
        private float _imageWidthRatio = 1f;
        private float _imageHeightRatio = 1f;

        private readonly string _initialDirectory =
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/KFT/tracker";

        private string _trackerMemoryFile = "";

        private int _trackerHandle;
        private Font _nameFont;
        private readonly StringFormat _nameFormat = new StringFormat { Alignment = StringAlignment.Center };
        private readonly Pen _limeGreenPen = new Pen(Color.LimeGreen, 2.5f);
        private readonly Pen _bluePen = new Pen(Color.Blue, 2.5f);

        private FpsCounter _fpsCounter = new FpsCounter();
        private int _mouseX;
        private int _mouseY;

        public FormFSDKTracked()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            FSDKFacePipeline.InitializeLibrary();
            _kinect = KinectInitializeHelper.InitializeKinect();
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

        private void button1_Click(object sender, EventArgs e)
        {
            if (_programState == ProgramState.Recognize || _programState == ProgramState.Name || _programState == ProgramState.Remember)
            {
                Pause();
                return;
            }

            if (_programState == ProgramState.Unitialized)
            {
                _colorFrameStream = _kinect.ColorFrameStream;
                _colorFrameStream.Open(true);
                _imageBuffer = new byte[_colorFrameStream.FrameDataSize];
                _imageWidthRatio = _colorFrameStream.FrameWidth / (float)pictureBox1.Width;
                _imageHeightRatio = _colorFrameStream.FrameHeight / (float)pictureBox1.Height;
                _nameFont = new Font("Arial", 12f * _imageWidthRatio);
                _programState = ProgramState.Stopped;
                statusLabel.Text = WaitingLabelText;


                FSDK.CreateTracker(ref _trackerHandle); // if could not be loaded, create a new _trackerHandle

                int err = 0;
                // ToString().ToLowerInvariant() needed because false.ToString() == "False", but FSDK expects "false"
                FSDK.SetTrackerMultipleParameters(_trackerHandle,
                    $"HandleArbitraryRotations={FSDKTrackedAppSettings.Default.FsdkHandleArbitraryRot.ToString().ToLowerInvariant()}; " +
                    $"DetermineFaceRotationAngle={FSDKTrackedAppSettings.Default.FsdkDetermineRotAngle.ToString().ToLowerInvariant()}; " +
                    $"InternalResizeWidth={FSDKTrackedAppSettings.Default.FsdkInternalResizeWidth}; " +
                    $"FaceDetectionThreshold={FSDKTrackedAppSettings.Default.FsdkFaceDetectionThreshold};",
                    ref err);
                _colorFrameStream.ColorFrameReady += ColorFrameStreamOnColorFrameReady;
            }

            if (_programState == ProgramState.Stopped)
            {
                Unpause();
            }
            
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


                var bitmap = imageBuffer.ToBitmap();
                imageBuffer.CreateFsdkImageHandle(out int fsdkImageHandle);
                var fsdkImage = new FSDK.CImage(fsdkImageHandle);

                long faceCount = 0;
                FSDK.FeedFrame(_trackerHandle, 0, fsdkImage.ImageHandle, ref faceCount, out var ids,
                    sizeof(long) * maximumFacesDetected);
                Array.Resize(ref ids, (int) faceCount);

                var graphics = Graphics.FromImage(bitmap);

                foreach (long id in ids)
                {
                    var facePosition = new FSDK.TFacePosition();
                    FSDK.GetTrackerFacePosition(_trackerHandle, 0, id, ref facePosition);

                    int left = facePosition.xc - (int) (facePosition.w * 0.6);
                    int top = facePosition.yc - (int) (facePosition.w * 0.5);
                    int w = (int) (facePosition.w * 1.2);

                    int res = FSDK.GetAllNames(_trackerHandle, id, out string name, 1024); // maximum of 1024 characters

                    if (FSDK.FSDKE_OK == res && name.Length > 0)
                    {
                        graphics.DrawString(name, _nameFont,
                            Brushes.LimeGreen,
                            facePosition.xc, top + w + 5, _nameFormat);
                    }

                    var pen = _limeGreenPen;
                    if (ProgramState.Name == _programState || _programState == ProgramState.Remember)
                    {
                        int mouseX = (int) (_mouseX * _imageWidthRatio);
                        int mouseY = (int) (_mouseY * _imageHeightRatio);
                        if (mouseX >= left && mouseX <= left + w && mouseY >= top && mouseY <= top + w)
                        {
                            pen = _bluePen;
                            if (FSDK.FSDKE_OK == FSDK.LockID(_trackerHandle, id))
                            {

                                if (_programState == ProgramState.Name)
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
                                            FSDK.SetName(_trackerHandle, id, userName);
                                        }
                                    }
                                }
                                else
                                {
                                    FSDK.SetName(_trackerHandle, id, id.ToString());
                                }

                                FSDK.UnlockID(_trackerHandle, id);

                                _programState = ProgramState.Recognize;
                            }

                        }
                    }

                    graphics.DrawRectangle(pen, left, top, w, w);
                }

                pictureBox1.Image = bitmap;
                _fpsCounter.NewFrame();
                statusLabel.Text = $"FPS: {_fpsCounter.CurrentFps:F2} (Mean {_fpsCounter.AverageFps:F2} Min {_fpsCounter.MinFps:F2} Max {_fpsCounter.MaxFps:F2})";

                Application.DoEvents();
            }
        }
    
    

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _programState = ProgramState.Name;
            }
            else if (e.Button == MouseButtons.Right)
            {
                _programState = ProgramState.Remember;
            }
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            _mouseX = e.X;
            _mouseY = e.Y;
        }

        private void pictureBox1_MouseLeave(object sender, EventArgs e)
        {
            _mouseX = 0;
            _mouseY = 0;
        }

        private void Pause()
        {
            if (_programState == ProgramState.Stopped) return;

            _kinect.Close();
            _programState = ProgramState.Stopped;
            startButton.Text = StartButtonStoppedText;
            statusLabel.Text = StoppedLabelText;
        }

        private void Unpause()
        {
            if(_programState != ProgramState.Stopped) return;

            _fpsCounter = new FpsCounter();
            _programState = ProgramState.Recognize;
            _kinect.Open();
            statusLabel.Text = WaitingLabelText;
            startButton.Text = StartButtonRunningText;
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
                FSDK.LoadTrackerMemoryFromFile(ref _trackerHandle, dialog.SafeFileName);
            }
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
            FSDK.ClearTracker(_trackerHandle);
            var err = 0;
            FSDK.SetTrackerMultipleParameters(_trackerHandle,
                $"HandleArbitraryRotations={FSDKTrackedAppSettings.Default.FsdkHandleArbitraryRot.ToString().ToLowerInvariant()}; " +
                $"DetermineFaceRotationAngle={FSDKTrackedAppSettings.Default.FsdkDetermineRotAngle.ToString().ToLowerInvariant()}; " +
                $"InternalResizeWidth={FSDKTrackedAppSettings.Default.FsdkInternalResizeWidth}; " +
                $"FaceDetectionThreshold={FSDKTrackedAppSettings.Default.FsdkFaceDetectionThreshold};",
                ref err);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }
    }
}
