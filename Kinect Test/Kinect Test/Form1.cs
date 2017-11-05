using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;
using KinectUnifier;
using LuxandFaceLib;

namespace Kinect_Test
{
    public partial class Form1 : Form
    {
        private Renderer _renderer;

        private IKinect _kinect;
        private ICoordinateMapper _coordinateMapper;

        private IColorManager _colorManager;
        private int _colorHeight;
        private int _colorWidth;
        private int _colorBytesPerPixel;
        private byte[] _colorFrameBuffer;

        private int _bodyCount;
        private IBody[] _bodies;
        private IBodyManager _bodyManager;

        private List<Rectangle> _facePositions;
        private List<double> _faceRotations;
        private LuxandFace2 _face;
        private LuxandFaceDatabase _faceDatabase = new LuxandFaceDatabase();

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

            _face = new LuxandFace2(_colorWidth, _colorHeight, _colorBytesPerPixel);
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
            _facePositions = new List<Rectangle>();
            _faceRotations = new List<double>();

            foreach (var body in _bodies)
            {
                if (Util.TryGetHeadRectangleInColorSpace(body, _coordinateMapper, out var faceRect, out double rotAngle))
                {
                    _facePositions.Add(faceRect);
                    _faceRotations.Add(rotAngle);
                }
            }
            _face.FeedFacePositions(_facePositions.ToArray());


            for (int i = 0; i < _facePositions.Count; i++)
            {
                _renderer.DrawFacialFeatures(_face.GetFacialFeatures(i), Brushes.Aqua, 1.5f);
                var matchedFace = _faceDatabase.GetBestMatch(_face.GetFaceTemplate(i));
                if(matchedFace.Item2 > 0.6f) { _renderer.DrawName($"{matchedFace.Item1} ({matchedFace.Item2:P})", _facePositions[i].Left, _facePositions[i].Bottom, _bodyBrushes[i]);}
            }
            pictureBox1.Invalidate();
            Application.DoEvents();
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

        private void pictureBox1_MouseClick(object sender, MouseEventArgs e)
        {
            var pointInColorCoordinates = Util.CoordinateSystemConversion(e.Location, pictureBox1.Width, pictureBox1.Height, _colorWidth, _colorHeight);
            for (var i = 0; i < _facePositions.Count; i++)
            {
                if (_facePositions[i].Contains(pointInColorCoordinates))
                {
                    var faceInfo = _face.GetFaceTemplate(i);
                    if (faceInfo == null) return;
                    if (!_faceDatabase.TryAddNewFace("Michal", faceInfo))
                    {
                        _faceDatabase.TryAddFaceTemplateToExistingFace("Michal", faceInfo);
                    }
                }
            }
        }
    }
}