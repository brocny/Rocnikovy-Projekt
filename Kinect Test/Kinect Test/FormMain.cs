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
    public partial class FormMain : Form
    {
        private ProgramState _programState;

        private readonly Renderer _renderer;

        private readonly IKinect _kinect;
        private readonly ICoordinateMapper _coordinateMapper;
        
        private int _colorHeight;
        private int _colorWidth;
        private int _colorBytesPerPixel;

        private FpsCounter _fpsCounter = new FpsCounter();

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

        public FormMain()
        {
            InitializeComponent();
            
            _kinect = KinectFactory.KinectFactory.GetKinect();
            _synchContext = TaskScheduler.FromCurrentSynchronizationContext();

            _faceDatabase = new FaceDatabase<byte[]>(new LuxandFaceInfo());
            _facePipeline = new LuxandFacePipeline(_faceDatabase);

            _facePipeline.FaceCuttingComplete += FacePipelineOnFaceCuttingComplete;

            LuxandFacePipeline.InitializeLibrary();
            _multiManager = _kinect.OpenMultiManager(MultiFrameTypes.Body | MultiFrameTypes.Color);
            _multiManager.MultiFrameArrived += MultiManagerOnMultiFrameArrived;

            InitializeColorComponents();

            _renderer = new Renderer(_colorWidth, _colorHeight);
            _bodyPens = _bodyBrushes.Select(br => new Pen(br, 2.5f)).ToArray();

            _coordinateMapper = _kinect.CoordinateMapper;
            _kinect.Open();
        }

        private void FacePipelineOnFaceCuttingComplete(object sender, FaceImage[] faceImages)
        {
            if (faceImages != null && faceImages.Length != 0)
            {
                var image = faceImages[0];
                facePictureBox.Image = image.Bitmap;
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

        void MultiFrameArrived_Test(object sender, MultiFrameReadyEventArgs e)
        {
            var multiFrame = e.MultiFrame;
            if (multiFrame == null) return;

            using (var colorFrame = multiFrame.ColorFrame)
            using (var bodyFrame = multiFrame.BodyFrame)
            {
                var bodyBuffer = new IBody[bodyFrame.BodyCount];
                var colorBuffer = new byte[colorFrame.PixelDataLength];

                bodyFrame.CopyBodiesTo(bodyBuffer);
                colorFrame.CopyFramePixelDataToArray(colorBuffer);

                _renderer.Image =
                    colorBuffer.BytesToBitmap(colorFrame.Width, colorFrame.Height, colorFrame.BytesPerPixel);

                _renderer.DrawBodies(bodyBuffer, _bodyBrushes, _bodyPens, _coordinateMapper);

                for (int i = 0; i < bodyBuffer.Length; i++)
                {
                    if (Util.TryGetHeadRectangle(bodyBuffer[i], _coordinateMapper, out var faceRect))
                    {
                        _renderer.DrawRectangles(new [] {faceRect}, _bodyPens);
                    }
                }

                _fpsCounter.NewFrame();
                pictureBox1.Image = _renderer.Image;
                statusLabel.Text = $"{_fpsCounter.Fps:F2} FPS";
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
                _programState = ProgramState.Paused;
            }
        }

        private void UnPause()
        {
            if (!_kinect.IsRunning)
            {
                _kinect.Open();
                button1.Text = "Stop";
                statusLabel.Text = "";
                _programState = ProgramState.Running;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Pause();
        }

        private void loadButton_Click(object sender, EventArgs e)
        {
            var originalState = _programState;
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

            if (originalState == ProgramState.Running) UnPause();
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            var originalState = _programState;
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
                        $"Error: An error occured while saving the database to {dialog.SelectedPath}: {Environment.NewLine} {exc}");
                }
            }

            if(originalState == ProgramState.Running) UnPause();
        }
    }
}

enum ProgramState
{
    Running, Paused
}