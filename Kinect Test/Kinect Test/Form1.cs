using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;
using KinectUnifier;

namespace Kinect_Test
{
    public partial class Form1 : Form
    {
        private Renderer _renderer;

        private IKinect _kinect;

        private int _bodyCount;

        private ICoordinateMapper _coordinateMapper;

        private IBody[] _bodies;

        private IBodyManager _bodyManager;
        private LuxandFace.LuxandFace _face;

        private byte[] _colorFrameBuffer;

        private IColorManager _colorManager;

        private int _colorHeight;
        private int _colorWidth;
        private int _colorBytesPerPixel;

        private readonly Brush[] _bodyBrushes =
        {
            Brushes.LimeGreen, Brushes.Blue, Brushes.Yellow, Brushes.Orange, Brushes.DeepPink,
            Brushes.Red
        };

        private Pen[] _bodyPens;
        
        
        
        public Form1()
        {
            InitializeComponent();
            
            _kinect = KinectFactory.KinectFactory.GetKinect();
            InitializeColorComponents();

            _face = new LuxandFace.LuxandFace(_colorWidth, _colorHeight, _colorBytesPerPixel);
            _face.InitializeLibrary();
            _renderer = new Renderer(new FormComponents(statusLabel, pictureBox1), _colorWidth, _colorHeight);

            InitializeBodyComponents();
            _bodyPens = _bodyBrushes.Select(br => new Pen(br, 2.5f)).ToArray();
            
            _coordinateMapper = _kinect.CoordinateMapper;
            _kinect.Open();
        }

      

        void InitializeColorComponents()
        {
            _colorManager = _kinect.ColorManager;
            _colorManager.Open(preferResolutionOverFps: true);
            _colorManager.ColorFrameReady += _colorManager_ColorFrameReady;
            _colorWidth = _colorManager.WidthPixels;
            _colorHeight = _colorManager.HeightPixels;
            _colorBytesPerPixel = _colorManager.BytesPerPixel;
        }

        private void _colorManager_ColorFrameReady(object sender, ColorFrameReadyEventArgs e)
        {
            using (var frame = e.ColorFrame)
            {
                if (frame == null) return;

                _colorBytesPerPixel = frame.BytesPerPixel;
                if (_colorFrameBuffer == null)
                {
                    _colorFrameBuffer = new byte[frame.PixelDataLength];
                }
                frame.CopyFramePixelDataToArray(_colorFrameBuffer);
            }
            
            _face.FeedFrame(_colorFrameBuffer);
        }

        void InitializeBodyComponents()
        {
            _bodyManager = _kinect.BodyManager;

            _bodyCount = _bodyManager.BodyCount;
            _bodies = new IBody[_bodyCount];

            _bodyManager.Open();
            _bodyManager.BodyFrameReady += _bodyManager_BodyFrameReady;
        }

        private void _bodyManager_BodyFrameReady(object sender, BodyFrameReadyEventArgs e)
        {
            using (var frame = e.BodyFrame)
            {
                if (frame == null) return;
                if (_bodies.Length < frame.BodyCount)
                {
                    Array.Resize(ref _bodies, frame.BodyCount);
                }
                frame.CopyBodiesTo(_bodies);
            }
            
            _renderer.ClearScreen();
            _renderer.DrawBodiesWithFaceBoxes(_bodies, _colorFrameBuffer, _colorBytesPerPixel, _bodyBrushes, _bodyPens, _coordinateMapper);


            List<Rectangle> faceRects = new List<Rectangle>();
            List<double> faceRots = new List<double>();

            foreach (var body in _bodies)
            {
                if (Util.TryGetHeadRectangleInColorSpace(body, _coordinateMapper, out var faceRect, out double rotAngle))
                {
                    faceRects.Add(faceRect);
                    faceRots.Add(rotAngle);
                }
            }
            _face.FeedFacePositions(faceRects.ToArray(), faceRots.ToArray());
            if(faceRects.Count > 0)
            
            _renderer.DrawFacialFeatures(_face.GetFacialFeatures(0), Brushes.Aqua, 1.5f);

            pictureBox1.Invalidate();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (_kinect.IsRunning)
            {
                _kinect.Close();
                button1.Text = "Start";
            }
            else
            {
                _kinect.Open();
                button1.Text = "Stop";
            }
        }
    }
}