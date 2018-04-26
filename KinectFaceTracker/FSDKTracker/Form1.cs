using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using KinectUnifier;
using LiveRecognition;
using Luxand;
using LuxandFaceLib;

namespace FSDKTracker
{
    public partial class Form1 : Form
    {
        // program states: whether we recognize faces, or user has clicked a face
        enum ProgramState { Remember, Recognize }
        ProgramState _programState = ProgramState.Recognize;


        private IKinect _kinect;
        private IColorManager _colorManager;
        private byte[] _imageBuffer;
        private bool _needClose;
        string _trackerMemoryFile = "tracker.dat";
        private int _trackerHandle;

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
            FSDK.SaveTrackerMemoryToFile(_trackerHandle, _trackerMemoryFile);
            FSDK.FreeTracker(_trackerHandle);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            _colorManager = _kinect.ColorManager;
            _kinect.Open();
            _colorManager.Open(true);
            _imageBuffer = new byte[_colorManager.FrameDataSize];

            // creating a Tracker
            if (FSDK.FSDKE_OK != FSDK.LoadTrackerMemoryFromFile(ref _trackerHandle, _trackerMemoryFile)
            ) // try to load saved tracker state
                FSDK.CreateTracker(ref _trackerHandle); // if could not be loaded, create a new _trackerHandle

            int err = 0; // set realtime face detection parameters
            FSDK.SetTrackerMultipleParameters(_trackerHandle,
                "HandleArbitraryRotations=false; DetermineFaceRotationAngle=false; InternalResizeWidth=200; FaceDetectionThreshold=4;",
                ref err);

            while (!_needClose)
            {
                Image image;
                using (var frame = _colorManager.GetNextFrame())
                {
                    if (frame == null)
                    {
                        Application.DoEvents();
                        continue;
                    }
                    frame.CopyFramePixelDataToArray(_imageBuffer);
                    image = _imageBuffer.BytesToBitmap(frame.Width, frame.Height, frame.BytesPerPixel);
                }

                var fsdkImage = new FSDK.CImage(image);

                long[] IDs;
                long faceCount = 0;
                FSDK.FeedFrame(_trackerHandle, 0, fsdkImage.ImageHandle, ref faceCount, out IDs,
                    sizeof(long) * 64); // maximum of 64 faces detected
                Array.Resize(ref IDs, (int) faceCount);

                // make UI controls accessible (to find if the user clicked on a face)
                Application.DoEvents();
                
                var graphics = Graphics.FromImage(image);

                for (int i = 0; i < IDs.Length; ++i)
                {
                    var facePosition = new FSDK.TFacePosition();
                    FSDK.GetTrackerFacePosition(_trackerHandle, 0, IDs[i], ref facePosition);

                    int left = facePosition.xc - (int) (facePosition.w * 0.6);
                    int top = facePosition.yc - (int) (facePosition.w * 0.5);
                    int w = (int) (facePosition.w * 1.2);

                    string name;
                    int res = FSDK.GetAllNames(_trackerHandle, IDs[i], out name, 1024); // maximum of 1024 characters

                    if (FSDK.FSDKE_OK == res && name.Length > 0)
                    {
                        // draw name
                        StringFormat format = new StringFormat {Alignment = StringAlignment.Center};

                        graphics.DrawString(name, new Font("Arial", 16),
                            Brushes.LightGreen,
                            facePosition.xc, top + w + 5, format);
                    }

                    Pen pen = Pens.LightGreen;
                    if (_mouseX >= left && _mouseX <= left + w && _mouseY >= top && _mouseY <= top + w)
                    {
                        pen = Pens.Blue;
                        if (ProgramState.Remember == _programState)
                        {
                            if (FSDK.FSDKE_OK == FSDK.LockID(_trackerHandle, IDs[i]))
                            {
                                // get the user name
                                InputName inputName = new InputName();
                                if (DialogResult.OK == inputName.ShowDialog())
                                {
                                    _userName = inputName.userName;
                                    if (string.IsNullOrEmpty(_userName))
                                    {
                                        string s = "";
                                        FSDK.SetName(_trackerHandle, IDs[i], "");
                                        FSDK.PurgeID(_trackerHandle, IDs[i]);
                                    }
                                    else
                                    {
                                        FSDK.SetName(_trackerHandle, IDs[i], _userName);
                                    }

                                    FSDK.UnlockID(_trackerHandle, IDs[i]);
                                }
                            }
                        }
                    }

                    graphics.DrawRectangle(pen, left, top, w, w);
                }

                _
                pictureBox1.Image = image;

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
