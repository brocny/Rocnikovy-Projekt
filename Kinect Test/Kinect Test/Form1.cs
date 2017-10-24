using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using KinectUnifier;
namespace Kinect_Test
{
    public partial class Form1 : Form
    {
        private Graphics _graphics;

        private const float JointSize = 8;

        private Bitmap _bmp;

        private IKinect _kinect;

        private int _bodyCount;

        private ICoordinateMapper _coordinateMapper;

        private IBody[] _bodies;

        private IBodyManager _bodyManager;

        private byte[] _colorFrameBuffer;

        private IColorManager _colorManager;

        private int _colorHeight;
        private int _colorWidth;
        private int _colorBytesPerPixel;

        private DateTime _lastColorFrameTime;

        private int _displayWidth;
        private int _displayHeight;

        private readonly Brush[] _bodyBrushes =
        {
            Brushes.LimeGreen, Brushes.Blue, Brushes.Yellow, Brushes.Orange, Brushes.DeepPink,
            Brushes.Red
        };

        private Pen[] _bodyPens;


        public void InitBones()
        {
            _bodyPens = new Pen[_bodyBrushes.Length];
            for (int i = 0; i < _bodyBrushes.Length; i++)
            {
                _bodyPens[i] = new Pen(_bodyBrushes[i], 6f);
            }
        }


        public Form1()
        {
            InitializeComponent();
            
            _kinect = KinectFactory.KinectFactory.GetKinect();
            InitializeColorComponents();

            InitializeDisplayComponents();

            InitializeBodyComponents();

            InitBones();

            _graphics.FillRectangle(Brushes.Black, pictureBox1.ClientRectangle);

            pictureBox1.Image = _bmp;
            _coordinateMapper = _kinect.CoordinateMapper;
            _kinect.Open();
        }

        void InitializeDisplayComponents()
        {
            _displayHeight = Math.Min(_colorManager.HeightPixels, pictureBox1.Height);
            _displayWidth = Math.Min(_colorManager.WidthPixels, pictureBox1.Width);
            _bmp = new Bitmap(_displayWidth, _displayHeight);
            _graphics = Graphics.FromImage(_bmp);
        }

        void InitializeColorComponents()
        {
            _colorManager = _kinect.ColorManager;
            _colorManager.Open(preferResolutionOverFps: true);
            _colorManager.ColorFrameReady += _colorManager_ColorFrameReady;
            _colorWidth = _colorManager.WidthPixels;
            _colorHeight = _colorManager.HeightPixels;
        }

        private void _colorManager_ColorFrameReady(object sender, ColorFrameReadyEventArgs e)
        {
            {
                using (var frame = e.ColorFrame)
                {
                    if (frame != null)
                    {
                        _colorBytesPerPixel = frame.BytesPerPixel;
                        if (_colorFrameBuffer == null)
                        {
                            _colorFrameBuffer = new byte[frame.PixelDataLength];
                        }
                        frame.CopyFramePixelDataToArray(_colorFrameBuffer);
                        _lastColorFrameTime = DateTime.Now;
                    }
                }
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

        private void _bodyManager_BodyFrameReady(object sender, BodyFrameReadyEventArgs e)
        {
            bool dataReceived = false;

            using (var frame = e.BodyFrame)
            {
                if (frame != null)
                {
                    if (_bodies.Length < frame.BodyCount)
                    {
                        _bodies = new IBody[frame.BodyCount];
                    }

                    frame.CopyBodiesTo(_bodies);
                    dataReceived = true;
                }
            }


            if (dataReceived)
            {
                // clear screen
                _graphics.FillRectangle(Brushes.DimGray, pictureBox1.ClientRectangle);

                for (int i = 0; i < _bodies.Length; i++)
                {
                    Dictionary<JointType, Point2F> jointColorSpacePoints =
                        new Dictionary<JointType, Point2F>();

                    foreach (var jointType in _bodies[i].Joints.Keys)
                    {
                        Point3F cameraPoint = _bodies[i].Joints[jointType].Position;
                        if (cameraPoint.Z < 0)
                        {
                            cameraPoint.Z = 0.1f;
                        }


                        Point2F colorPoint =
                            _coordinateMapper.MapCameraPointToColorSpace(_bodies[i].Joints[jointType].Position);

                        jointColorSpacePoints.Add(jointType, colorPoint);
                        var pointX = colorPoint.X * _displayWidth / _colorWidth - JointSize / 2;
                        var pointY = colorPoint.Y * _displayHeight / _colorHeight - JointSize / 2;
                        if (pointX >= 0 && pointX <= _displayWidth && pointY >= 0 && pointY < _displayHeight)
                        {
                            _graphics.FillEllipse(_bodyBrushes[i], pointX, pointY, JointSize, JointSize);
                        }
                        

                        if (jointType == JointType.Head && colorPoint.X >= 0 && colorPoint.Y >= 0)
                        {
                            // Draw picture around head
                            DrawColorBoxAroundPoint(
                                colorPoint,
                                (int) (_kinect.ColorManager.WidthPixels / (cameraPoint.Z * 6)),
                                (int) (_kinect.ColorManager.HeightPixels / (cameraPoint.Z * 3))
                                );    
                        }
                    }

                    foreach (var bone in _bodies[i].Bones)
                    {
                        DrawBone(_bodies[i].Joints, jointColorSpacePoints, bone.Item1, bone.Item2, i);
                    }
                }
            }

            pictureBox1.Invalidate();
        }

        private void DrawColorBoxAroundPoint(Point2F colorPoint, int boxWidth = 160, int boxHeight = 200)
        {
            if (boxWidth == 0 || boxHeight == 0 || _colorFrameBuffer == null)
                return;

            var x = (int) (colorPoint.X - boxWidth / 2);
            if (x < 0) x = 0;
            if (x > _colorWidth) x = _colorWidth;

            var y = (int) (colorPoint.Y - boxHeight * 6 / 11);
            if (y < 0) y = 0;
            if (y > _colorHeight) y = _colorHeight;

            var width = Math.Min(_colorWidth - x, boxWidth);
            var height = Math.Min(_colorHeight - y, boxHeight);

            var bmpX = x * _displayWidth / _colorWidth;
            var bmpY = y * _displayHeight / _colorHeight;
            var bmpWidth = width * _displayWidth / _colorWidth;
            var bmpHeight = height * _displayHeight / _colorHeight;

            var buffer = _colorFrameBuffer;

            if (bmpWidth == 0 || bmpHeight == 0)
                return;

            var bmpData = _bmp.LockBits(new Rectangle(bmpX, bmpY, bmpWidth, bmpHeight),
                ImageLockMode.WriteOnly,
                _bmp.PixelFormat);
            unsafe
            {
                byte* bmpPointer = (byte*) bmpData.Scan0;
                for (int i = 0; i < height; ++i)
                {
                    int bufAddr = _colorBytesPerPixel * ((y + i) * _colorWidth + x);
                    int bmpLineAddr = i * _displayHeight / _colorHeight * bmpData.Stride;
                    for (int j = 0; j < width; ++j)
                    {
                        var bmpAddr = bmpLineAddr + j * _displayWidth / _colorWidth * 4;  
                        bmpPointer[bmpAddr] = buffer[bufAddr];
                        bmpPointer[bmpAddr + 1] = buffer[bufAddr + 1];
                        bmpPointer[bmpAddr + 2] = buffer[bufAddr + 2];

                        bufAddr += _colorBytesPerPixel;
                    }
                }
            } 
            _bmp.UnlockBits(bmpData);
            statusLabel.Text = $"FPS: {1000f / (DateTime.Now - _lastColorFrameTime).Milliseconds:F2}";
        }

        private void DrawBone(IReadOnlyDictionary<JointType, IJoint> joints,
            IDictionary<JointType, Point2F> jointColorSpacePoints, JointType jointType0, JointType jointType1,
            int bodyIndex)
        {
            IJoint joint0;
            IJoint joint1;
            if (!joints.TryGetValue(jointType0, out joint0) || !joints.TryGetValue(jointType1, out joint1))
            {
                return;
            }
            
            

            // If we can't find either of these joints, exit
            if (!joint1.IsTracked || !joint0.IsTracked)
            {
                return;
            }

            _graphics.DrawLine(_bodyPens[bodyIndex], 
                jointColorSpacePoints[jointType0].X * _displayWidth / _colorWidth,
                jointColorSpacePoints[jointType0].Y * _displayHeight / _colorHeight,
                jointColorSpacePoints[jointType1].X * _displayWidth / _colorWidth,
                jointColorSpacePoints[jointType1].Y * _displayHeight / _colorHeight
                );
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