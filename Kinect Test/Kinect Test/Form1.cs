using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;
using KinectUnifier;
using LuxandFaceLib;
using System.Threading.Tasks;
using Face;

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
        private byte[] _previousFrameBuffer;

        private int _bodyCount;
        private IBody[] _bodies;
        private IBody[] _previousBodies;
        private IBodyManager _bodyManager;

        private List<Rectangle> _faceRectangles;
        private List<double> _faceRotations;
        private LuxandFace2 _face;
        private LuxandFace2 _face2;
        private FaceDatabase<byte[]> _faceDatabase;

        private Task _lastTask;
        //private TaskScheduler _synchContext = TaskScheduler.FromCurrentSynchronizationContext();

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

            _faceDatabase = new FaceDatabase<byte[]>(new LuxandFaceInfo());

            _face2 = new LuxandFace2(_colorWidth, _colorHeight, _colorBytesPerPixel);
            //_face2.InitializeLibrary();

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
                if(_colorFrameBuffer == null || _colorFrameBuffer.Length != frame.PixelDataLength)
                    _colorFrameBuffer = new byte[frame.PixelDataLength];

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
            var temp = _face;
            _face = _face2;
            _face2 = temp;
        }

        private async void _bodyManager_BodyFrameReady(object sender, BodyFrameReadyEventArgs e)
        {
            if(_lastTask != null)
                await _lastTask;

            using (var frame = e.BodyFrame)
            {
                if (frame == null) return;
                if (_bodies.Length < frame.BodyCount)
                {
                    Array.Resize(ref _bodies, frame.BodyCount);
                }
                frame.CopyBodiesTo(_bodies);
            }

            if (_colorFrameBuffer == null) return;

            var renderTask = Task.Run(() =>
            {
                _renderer.ClearScreen();
                _renderer.DrawBodiesWithFaceBoxes(_previousBodies, _previousFrameBuffer, _colorBytesPerPixel, _bodyBrushes,
                    _bodyPens, _coordinateMapper);
            });

            SwapFaceLibs();

            var feedTask = Task.Run(() =>
            {
                _faceRectangles = new List<Rectangle>();
                foreach (var body in _bodies)
                {
                    if (Util.TryGetHeadRectangle(body, _coordinateMapper, out var faceRect))
                    {
                        _faceRectangles.Add(faceRect);
                    }
                }
                _face.FeedFaces(_colorFrameBuffer, _faceRectangles.ToArray(), _colorBytesPerPixel);
            });

            Point[][] features = new Point[_face2.FaceCount][];

            var featureTask = Task.Run(() =>
            {
                Parallel.For(0, _face2.FaceCount, i =>
                {
                    features[i] = _face2.GetFacialFeatures(i);
                });
            });

            var copyBufferTask = renderTask.ContinueWith(t =>
            {
                _previousFrameBuffer = _previousFrameBuffer ?? new byte[_colorFrameBuffer.Length];
                _colorFrameBuffer.CopyTo(_previousFrameBuffer, 0);

                _previousBodies = _previousBodies ?? new IBody[_bodies.Length];
                _bodies.CopyTo(_previousBodies, 0);
            });
            
            var renderFeaturesTask = Task.WhenAll(featureTask, renderTask).ContinueWith(t =>
            {
            });

            var matchingTask = renderFeaturesTask.ContinueWith(t =>
            {
                for (int i = 0; i < _face2.FaceCount; i++)
                {
                    var matchedFace = _faceDatabase.GetBestMatch(_face2.GetFaceTemplate(i));
                    if (matchedFace.confidence > FaceMatchConfidenceThreshold)
                    {
                        _renderer.DrawName(
                            $"{matchedFace.name} ({matchedFace.confidence:P})",
                            _face.FaceRectangles[i].Left,
                            _face.FaceRectangles[i].Bottom,
                            _bodyBrushes[i]);
                    }
                }
            });

            _lastTask = Task.WhenAll(matchingTask, copyBufferTask);
            await _lastTask;
            Invoke((Action)(() =>
            {
                pictureBox1.Image = _renderer.Image;
                pictureBox1.Refresh();
            }));
            
            await feedTask;
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
            _lastTask.ContinueWith(t =>
            {
                var pointInColorCoordinates = Util.CoordinateSystemConversion(e.Location, pictureBox1.Width,
                    pictureBox1.Height, _colorWidth, _colorHeight);
                for (var i = 0; i < _face2.FaceCount; i++)
                {
                    if (_faceRectangles[i].Contains(pointInColorCoordinates))
                    {
                        var index = i;
                        var faceInfo = _face2.GetFaceTemplate(index);
                        if (faceInfo == null) return;
                        if (!_faceDatabase.TryAddNewFace("Michal", faceInfo))
                        {
                            _faceDatabase.TryAddFaceTemplateToExistingFace("Michal", faceInfo);
                        }
                    }
                }
            }, TaskContinuationOptions.AttachedToParent);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            _kinect.Close();
        }
    }
}