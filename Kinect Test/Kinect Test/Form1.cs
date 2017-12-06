using System;
using System.Collections.Generic;
using System.Drawing;
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
        private readonly Renderer _renderer;

        private readonly IKinect _kinect;
        private readonly ICoordinateMapper _coordinateMapper;
        
        private int _colorHeight;
        private int _colorWidth;
        private int _colorBytesPerPixel;
        private byte[] _colorFrameBuffer;

        private int _bodyCount;
        private IBody[] _bodies;

        private List<Rectangle> _faceRectangles;
        private LuxandFacePipeline _facePipeline;
        private readonly FaceDatabase<byte[]> _faceDatabase;

        private IMultiManager _multiManager;

        private TaskScheduler _synchContext;

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
            _synchContext = TaskScheduler.FromCurrentSynchronizationContext();

            InitializeColorComponents();

            _faceDatabase = new FaceDatabase<byte[]>(new LuxandFaceInfo());
            _facePipeline = new LuxandFacePipeline(_faceDatabase);

            LuxandFacePipeline.InitializeLibrary();
            _multiManager = _kinect.OpenMultiManager(MultiFrameTypes.Body | MultiFrameTypes.Color);
            _multiManager.MultiFrameArrived += MultiManagerOnMultiFrameArrived;

            _renderer = new Renderer(new FormComponents(statusLabel, pictureBox1), _colorWidth, _colorHeight);
            _bodyPens = _bodyBrushes.Select(br => new Pen(br, 2.5f)).ToArray();
            
            _coordinateMapper = _kinect.CoordinateMapper;
            _kinect.Open();
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
                var createBitMapTask = locTask.ContinueWith(t => t.Result.ColorBuffer.BytesToBitmap(t.Result.Width, t.Result.Height, t.Result.BytesPerPixel));
                var renderTask = createBitMapTask.ContinueWith(t =>
                {
                    _renderer.Image = t.Result;
                    _renderer.DrawBodies(locTask.Result.Bodies, _bodyBrushes, _bodyPens, _coordinateMapper);
                    _renderer.DrawRectangles(locTask.Result.FaceRectangles, _bodyPens);
                    return _renderer.Image;
                });

                pictureBox1.Image = renderTask.Result;
            }
        }

        void InitializeColorComponents()
        {
            _colorWidth = _kinect.ColorManager.HeightPixels;
            _colorHeight = _kinect.ColorManager.WidthPixels;
            _colorBytesPerPixel = _kinect.ColorManager.BytesPerPixel;
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
            /*
            _lastTask?.ContinueWith(t =>
            {
                var pointInColorCoordinates = Util.CoordinateSystemConversion(e.Location, pictureBox1.Width,
                    pictureBox1.Height, _colorWidth, _colorHeight);
                for (var i = 0; i < _face2.FaceCount; i++)
                {
                    if (_faceRectangles[i].Contains(pointInColorCoordinates))
                    {
                        var index = i;
                        var faceTemplate = _face2.GetFaceTemplate(index);
                        if (faceTemplate == null) return;
                        _faceDatabase.Add(10, faceTemplate);
                    }
                }
            }, TaskContinuationOptions.AttachedToParent);
            */
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            _kinect.Close();
        }

        private void faceNameTextBox_Enter(object sender, EventArgs e)
        {
            faceNameTextBox.Text = "";
            faceNameTextBox.ForeColor = Color.Black;
        }
    }
}