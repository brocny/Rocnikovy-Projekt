using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using KinectFaceTracker;
using KinectUnifier;
using App;
using Luxand;
using LuxandFaceLib;

namespace FSDKTracker
{
    public partial class Form1 : Form
    {
        // program states: whether we recognize faces, or user has clicked a face
        private enum ProgramState { Remember, Recognize }

        private ProgramState _programState = ProgramState.Recognize;

        private IKinect _kinect;
        private IColorManager _colorManager;
        private byte[] _imageBuffer;
        private float _imageWidthRatio = 1f;
        private float _imageHeightRatio = 1f;

        private const string TrackerMemoryFile = "tracker.dat";
        private int _trackerHandle;
        private Font _nameFont;
        private readonly StringFormat _nameFormat = new StringFormat { Alignment = StringAlignment.Center };

        private readonly FpsCounter _fpsCounter = new FpsCounter();
        private bool _needClose;
        private int _mouseX;
        private int _mouseY;
        private string _userName;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            FSDKFacePipeline.InitializeLibrary();
            _kinect = KinectFactory.KinectFactory.GetKinect();
        }
      

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            _needClose = true;
            FSDK.SaveTrackerMemoryToFile(_trackerHandle, TrackerMemoryFile);
            FSDK.FreeTracker(_trackerHandle);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            _colorManager = _kinect.ColorManager;
            _kinect.Open();
            _colorManager.Open(true);
            _imageBuffer = new byte[_colorManager.FrameDataSize];
            _imageWidthRatio = _colorManager.FrameWidth / (float)pictureBox1.Width;
            _imageHeightRatio = _colorManager.FrameHeight / (float)pictureBox1.Height;
            _nameFont = new Font("Arial", 12f * _imageWidthRatio);

            // creating a Tracker
            if (FSDK.FSDKE_OK != FSDK.LoadTrackerMemoryFromFile(ref _trackerHandle, TrackerMemoryFile)
            ) // try to load saved tracker state
                FSDK.CreateTracker(ref _trackerHandle); // if could not be loaded, create a new _trackerHandle

            int err = 0; // set realtime face detection parameters
            FSDK.SetTrackerMultipleParameters(_trackerHandle,
                "HandleArbitraryRotations=false; DetermineFaceRotationAngle=false; InternalResizeWidth=540; FaceDetectionThreshold=4;",
                ref err);

            TrackingLoop();
        }

        private void TrackingLoop()
        {
            Image bitmap;
            FSDK.CImage fsdkImage = new FSDK.CImage();
            int imageHandle = 0;

            while (!_needClose)
            {
                using (var frame = _colorManager.GetNextFrame())
                {
                    if (frame == null)
                    {
                        Application.DoEvents();
                        continue;
                    }
                    frame.CopyFramePixelDataToArray(_imageBuffer);
                    bitmap = _imageBuffer.BytesToBitmap(frame.Width, frame.Height, frame.BytesPerPixel);
                }
                
                fsdkImage = new FSDK.CImage(bitmap);

                long faceCount = 0;
                FSDK.FeedFrame(_trackerHandle, 0, fsdkImage.ImageHandle, ref faceCount, out var ids,
                    sizeof(long) * 64); // maximum of 64 faces detected
                Array.Resize(ref ids, (int)faceCount);

                // make UI controls accessible (to find if the user clicked on a face)
                Application.DoEvents();

                var graphics = Graphics.FromImage(bitmap);

                foreach (long id in ids)
                {
                    var facePosition = new FSDK.TFacePosition();
                    FSDK.GetTrackerFacePosition(_trackerHandle, 0, id, ref facePosition);

                    int left = facePosition.xc - (int)(facePosition.w * 0.6);
                    int top = facePosition.yc - (int)(facePosition.w * 0.5);
                    int w = (int)(facePosition.w * 1.2);

                    int res = FSDK.GetAllNames(_trackerHandle, id, out string name, 1024); // maximum of 1024 characters

                    if (FSDK.FSDKE_OK == res && name.Length > 0)
                    {
                        graphics.DrawString(name, _nameFont,
                            Brushes.LimeGreen,
                            facePosition.xc, top + w + 5, _nameFormat);
                    }

                    Pen pen = Pens.LimeGreen;
                    if (ProgramState.Remember == _programState)
                    {
                        int mouseX = (int)(_mouseX * _imageWidthRatio);
                        int mouseY = (int)(_mouseY * _imageHeightRatio);
                        if (mouseX >= left && mouseX <= left + w && mouseY >= top && mouseY <= top + w)
                        {
                            pen = Pens.Blue;
                            if (FSDK.FSDKE_OK == FSDK.LockID(_trackerHandle, id))
                            {
                                // get the user name
                                var inputNameForm = new InputNameForm();
                                if (DialogResult.OK == inputNameForm.ShowDialog())
                                {
                                    _userName = inputNameForm.UserName;
                                    if (string.IsNullOrEmpty(_userName))
                                    {
                                        FSDK.SetName(_trackerHandle, id, "");
                                        FSDK.PurgeID(_trackerHandle, id);
                                    }
                                    else
                                    {
                                        FSDK.SetName(_trackerHandle, id, _userName);
                                    }

                                    FSDK.UnlockID(_trackerHandle, id);
                                }
                            }

                        }
                    }

                    graphics.DrawRectangle(pen, left, top, w, w);
                    //FSDK.FreeImage(imageHandle);
                }

                pictureBox1.Image = bitmap;
                _fpsCounter.NewFrame();
                fpsLabel.Text = $"{_fpsCounter.Fps:F2} FPS";

                _programState = ProgramState.Recognize;
                GC.Collect(); // collect the garbage after the deletion 
            }
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            _programState = ProgramState.Remember;
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
    }
}
