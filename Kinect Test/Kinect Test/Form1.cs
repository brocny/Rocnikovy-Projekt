using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using KinectUnifier;

namespace Kinect_Test
{
    public partial class Form1 : Form
    {
        private Graphics _graphics;

        private const int JointSize = 7;

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

        private DateTime _lastColorFrameTime;


        private List<Tuple<JointType, JointType>> _bones;

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
            _bones = new List<Tuple<JointType, JointType>>
            {
                new Tuple<JointType, JointType>(JointType.Head, JointType.Neck),

                // Torso
                new Tuple<JointType, JointType>(JointType.Neck, JointType.ShoulderCenter),
                new Tuple<JointType, JointType>(JointType.ShoulderCenter, JointType.SpineMid),
                new Tuple<JointType, JointType>(JointType.SpineMid, JointType.HipCenter),
                new Tuple<JointType, JointType>(JointType.ShoulderCenter, JointType.ShoulderRight),
                new Tuple<JointType, JointType>(JointType.ShoulderCenter, JointType.ShoulderLeft),
                new Tuple<JointType, JointType>(JointType.HipCenter, JointType.HipRight),
                new Tuple<JointType, JointType>(JointType.HipCenter, JointType.HipLeft),

                // Right Arm
                new Tuple<JointType, JointType>(JointType.ShoulderRight, JointType.ElbowRight),
                new Tuple<JointType, JointType>(JointType.ElbowRight, JointType.WristRight),
                new Tuple<JointType, JointType>(JointType.WristRight, JointType.HandRight),

                // Left Arm
                new Tuple<JointType, JointType>(JointType.ShoulderLeft, JointType.ElbowLeft),
                new Tuple<JointType, JointType>(JointType.ElbowLeft, JointType.WristLeft),
                new Tuple<JointType, JointType>(JointType.WristLeft, JointType.HandLeft),

                // Right Leg
                new Tuple<JointType, JointType>(JointType.HipRight, JointType.KneeRight),
                new Tuple<JointType, JointType>(JointType.KneeRight, JointType.AnkleRight),
                new Tuple<JointType, JointType>(JointType.AnkleRight, JointType.FootRight),

                // Left Leg
                new Tuple<JointType, JointType>(JointType.HipLeft, JointType.KneeLeft),
                new Tuple<JointType, JointType>(JointType.KneeLeft, JointType.AnkleLeft),
                new Tuple<JointType, JointType>(JointType.AnkleLeft, JointType.FootLeft)
            };

            _bodyPens = new Pen[_bodyBrushes.Length];
            for (int i = 0; i < _bodyBrushes.Length; i++)
            {
                _bodyPens[i] = new Pen(_bodyBrushes[i], 1f);
            }
        }


        public Form1()
        {
            InitializeComponent();



            _kinect = KinectFactory.KinectFactory.GetKinect360();
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
            _colorManager.Open(true);
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
                    if (_bodies == null)
                    {
                        _bodies = new IBody[frame.BodyCount];
                    }

                    frame.CopyBodiesTo(_bodies);
                    dataReceived = true;
                }
            }


            if (dataReceived)
            {
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
                        if (jointType == JointType.Head && colorPoint.X >= 0 && colorPoint.Y >= 0)
                        {
                            DrawColorBoxAroundPoint(colorPoint, (int) (300 / cameraPoint.Z), (int) (350 / cameraPoint.Z));
                        }

                        _graphics.FillEllipse(_bodyBrushes[i], colorPoint.X * _displayWidth / _colorWidth,
                            colorPoint.Y * _displayHeight / _colorHeight, JointSize, JointSize);
                    }

                    foreach (var bone in _bones)
                    {
                        DrawBone(_bodies[i].Joints, jointColorSpacePoints, bone.Item1, bone.Item2, i);
                    }
                }
            }

            pictureBox1.Invalidate();
        }

        private void DrawColorBoxAroundPoint(Point2F colorPoint, int boxWidth = 160, int boxHeight = 200)
        {
            if (_colorFrameBuffer != null)
            {
                var x = (int) (colorPoint.X - boxWidth / 2);
                if (x < 0) x = 0;
                if (x > _colorWidth) x = _colorWidth;

                var y = (int) (colorPoint.Y - boxHeight * 6 / 11);
                if (y < 0) y = 0;
                if (y > _colorHeight) y = _colorHeight;

                var width = Math.Min(_colorWidth - x, boxWidth);
                var height = Math.Min(_colorHeight - y, boxHeight);

                var buffer = _colorFrameBuffer;
                for (int i = 0; i < width; i++)
                {
                    for (int j = 0; j < height; j++)
                    {
                        int bufferAddr = 4 * ((y + j) * _colorWidth + x + i);
                        Color pixelColor = Color.FromArgb(255, buffer[bufferAddr], buffer[bufferAddr + 1],
                            buffer[bufferAddr + 2]);
                        // TODO: Fast pixel data access via pointers
                        _bmp.SetPixel((x + i) * _displayWidth / _colorWidth, (y + j) * _displayHeight / _colorHeight,
                            pixelColor);
                    }
                }
                statusLabel.Text = string.Format("FPS: {0:F2}",
                    (1000f / (DateTime.Now - _lastColorFrameTime).Milliseconds));
            }
        }

        private void DrawBone(IReadOnlyDictionary<JointType, IJoint> joints,
            IDictionary<JointType, Point2F> jointColorSpacePoints, JointType jointType0, JointType jointType1,
            int bodyIndex)
        {
            IJoint joint0 = joints[jointType0];
            IJoint joint1 = joints[jointType1];

            // If we can't find either of these joints, exit
            if (!joint1.IsTracked || !joint0.IsTracked)
            {
                return;
            }

            _graphics.DrawLine(_bodyPens[bodyIndex], jointColorSpacePoints[jointType0].X * _displayWidth / _colorWidth,
                jointColorSpacePoints[jointType0].Y * _displayHeight / _colorHeight,
                jointColorSpacePoints[jointType1].X * _displayWidth / _colorWidth,
                jointColorSpacePoints[jointType1].Y * _displayHeight / _colorHeight);
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