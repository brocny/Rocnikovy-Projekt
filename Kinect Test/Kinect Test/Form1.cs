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

        private FpsCounter _fpsCounter = new FpsCounter();

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

            _facePipeline.FaceCuttingComplete += FacePipelineOnFaceCuttingComplete;

            LuxandFacePipeline.InitializeLibrary();
            _multiManager = _kinect.OpenMultiManager(MultiFrameTypes.Body | MultiFrameTypes.Color);
            _multiManager.MultiFrameArrived += MultiManagerOnMultiFrameArrived;

            _renderer = new Renderer(new FormComponents(statusLabel, pictureBox1), _colorWidth, _colorHeight);
            _bodyPens = _bodyBrushes.Select(br => new Pen(br, 2.5f)).ToArray();
            
            _coordinateMapper = _kinect.CoordinateMapper;
            _kinect.Open();
        }

        private void FacePipelineOnFaceCuttingComplete(object sender, FaceImage[] faceImages)
        {
            if (faceImages?.Length != 0)
            {
                var image = faceImages[0];
                facePictureBox.Image = image.PixelBuffer.BytesToBitmap(image.Width, image.Height, image.BytesPerPixel);
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
                var createBitMapTask = locTask.ContinueWith(t => t.Result.ColorBuffer.BytesToBitmap(t.Result.Width, t.Result.Height, t.Result.BytesPerPixel));
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
                Pause();
            }
            else
            {
                UnPause();                
            }
        }

        private void Pause()
        {
            if (_kinect.IsRunning)
            {
                _kinect.Close();
                button1.Text = "Start";
                statusLabel.Text = "STOPPED";
            }
        }

        private void UnPause()
        {
            if (!_kinect.IsRunning)
            {
                _kinect.Open();
                button1.Text = "Stop";
                statusLabel.Text = "";
            }
        }

        private void pictureBox1_MouseClick(object sender, MouseEventArgs e)
        {

        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Pause();
        }

        private void loadButton_Click(object sender, EventArgs e)
        {
            Pause();
            var dialog = new FolderBrowserDialog
            {
                Description = "Select folder containing face database data",
                ShowNewFolderButton = false,
                SelectedPath = Environment.CurrentDirectory
            };

            var result = dialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                try
                {
                    _faceDatabase.Deserialize(dialog.SelectedPath);
                }
                catch (Exception exc)
                {
                    MessageBox.Show(
                        $"Error: An error occured while loading the database from {dialog.SelectedPath}: {Environment.NewLine} {exc}");
                }
            }
            UnPause();
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            Pause();
            var dialog = new FolderBrowserDialog()
            {
                Description = "Select folder to save Face data to",
                ShowNewFolderButton = true,
                SelectedPath = Environment.CurrentDirectory
            };

            var result = dialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                try
                {
                    _faceDatabase.Serialize(dialog.SelectedPath);
                }
                catch (Exception exc)
                {
                    MessageBox.Show(
                        $"Error: An error occured while the database to {dialog.SelectedPath}: {Environment.NewLine} {exc}");
                }
            }
            UnPause();
        }
    }
}