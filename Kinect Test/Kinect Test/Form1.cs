using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;
using KinectUnifier;
using LuxandFaceLib;
using System.Threading.Tasks;

namespace Kinect_Test
{
    public partial class Form1 : Form
    {
        // Confidence of face match has to be above this value do display a name
        private const float FaceMatchConfidenceThreshold = 0.75f;

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
        private LuxandFace2 _face1;
        private LuxandFace2 _face2;
        private LuxandFaceDatabase _faceDatabase = new LuxandFaceDatabase();

        private Task _lastTask;

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

            _face1 = new LuxandFace2(_colorWidth, _colorHeight, _colorBytesPerPixel);
            _face1.InitializeLibrary();

            _face2 = new LuxandFace2(_colorWidth, _colorHeight, _colorBytesPerPixel);
            _face2.InitializeLibrary();

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
        }

        void InitializeBodyComponents()
        {
            _bodyManager = _kinect.BodyManager;

            _bodyCount = _bodyManager.BodyCount;
            _bodies = new IBody[_bodyCount];

            _bodyManager.Open();
            _bodyManager.BodyFrameReady += _bodyManager_BodyFrameReady;
        }

        private void SwapFaceLibs()
        {
            var temp = _face1;
            _face1 = _face2;
            _face2 = temp;
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
            _renderer.DrawBodiesWithFaceBoxes(_bodies, _colorFrameBuffer, _colorBytesPerPixel, _bodyBrushes,
                _bodyPens, _coordinateMapper);
            
            _lastTask.Wait();
            
            _facePositions = new List<Rectangle>();

            foreach (var body in _bodies)
            {
                if (Util.TryGetHeadRectangle(body, _coordinateMapper, out var faceRect))
                {
                    _facePositions.Add(faceRect);
                }
            }

            SwapFaceLibs();

            var feedTask = Task.Run(() =>
            {
                _face1.FeedFaces(_colorFrameBuffer, _facePositions.ToArray(), _colorBytesPerPixel);
            });

            var featuresTask = Task.Run(() =>
            {
                var result = new Point[_face2.FaceCount][];
                for (int i = 0; i < _face2.FaceCount; i++)
                {
                    result[i] = _face2.GetFacialFeatures(i);
                }
                return result;
            });

            var drawFeaturesTask = featuresTask.ContinueWith(t =>
            {
                var features = t.Result;
                for (int i = 0; i < features.Length; i++)
                {
                    _renderer.DrawFacialFeatures(features[i], _bodyBrushes[i], 1.5f);
                }
            }, TaskContinuationOptions.HideScheduler);

            var matchingTask = featuresTask.ContinueWith(t =>
            {
                for (int i = 0; i < _face2.FaceCount; i++)
                {
                    var matchedFace = _faceDatabase.GetBestMatch(_face1.GetFaceTemplate(i));
                    if (matchedFace.Item2 > FaceMatchConfidenceThreshold)
                    {
                        _renderer.DrawName(
                        $"{matchedFace.name} ({matchedFace.confidence:P})",
                        _facePositions[i].Left,
                        _facePositions[i].Bottom,
                        _bodyBrushes[i]);
                    }
                }
            }, TaskContinuationOptions.HideScheduler);

            _lastTask = Task.WhenAll(matchingTask, drawFeaturesTask);

            pictureBox1.Invalidate();
            Application.DoEvents();

            feedTask.Wait();
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
                    var faceInfo = _face1.GetFaceTemplate(i);
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