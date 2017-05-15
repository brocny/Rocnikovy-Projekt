using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Kinect;

namespace Kinect_Test
{
    public partial class Form1 : Form
    {
        private Graphics gr;

        private const int JointSize = 7;

        private Bitmap bmp;

        private KinectSensor kinect;

        private int bodyCount;

        private CoordinateMapper coordinateMapper;

        private Body[] bodies;

        private BodyFrameSource bodyFrameSource;
        private BodyFrameReader bodyFrameReader;

        private byte[] colorFrameBuffer;

        private ColorFrameSource colorFrameSource;
        private ColorFrameReader colorFrameReader;
        private ColorFrame colorFrame;

        private int colorHeight;
        private int colorWidth;

        private DateTime lastColorFrameTime;


        private List<Tuple<JointType, JointType>> bones;

        private int displayWidth;
        private int displayHeight;

        private Brush[] bodyBrushes =
        {
            Brushes.LimeGreen, Brushes.Blue, Brushes.Yellow, Brushes.Orange, Brushes.DeepPink,
            Brushes.Red
        };

        private Pen[] bodyPens;


        public void InitBones()
        {
            bones = new List<Tuple<JointType, JointType>>
            {
                new Tuple<JointType, JointType>(JointType.Head, JointType.Neck),

                // Torso
                new Tuple<JointType, JointType>(JointType.Neck, JointType.SpineShoulder),
                new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.SpineMid),
                new Tuple<JointType, JointType>(JointType.SpineMid, JointType.SpineBase),
                new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderRight),
                new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderLeft),
                new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipRight),
                new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipLeft),

                // Right Arm
                new Tuple<JointType, JointType>(JointType.ShoulderRight, JointType.ElbowRight),
                new Tuple<JointType, JointType>(JointType.ElbowRight, JointType.WristRight),
                new Tuple<JointType, JointType>(JointType.WristRight, JointType.HandRight),
                new Tuple<JointType, JointType>(JointType.HandRight, JointType.HandTipRight),
                new Tuple<JointType, JointType>(JointType.WristRight, JointType.ThumbRight),

                // Left Arm
                new Tuple<JointType, JointType>(JointType.ShoulderLeft, JointType.ElbowLeft),
                new Tuple<JointType, JointType>(JointType.ElbowLeft, JointType.WristLeft),
                new Tuple<JointType, JointType>(JointType.WristLeft, JointType.HandLeft),
                new Tuple<JointType, JointType>(JointType.HandLeft, JointType.HandTipLeft),
                new Tuple<JointType, JointType>(JointType.WristLeft, JointType.ThumbLeft),

                // Right Leg
                new Tuple<JointType, JointType>(JointType.HipRight, JointType.KneeRight),
                new Tuple<JointType, JointType>(JointType.KneeRight, JointType.AnkleRight),
                new Tuple<JointType, JointType>(JointType.AnkleRight, JointType.FootRight),

                // Left Leg
                new Tuple<JointType, JointType>(JointType.HipLeft, JointType.KneeLeft),
                new Tuple<JointType, JointType>(JointType.KneeLeft, JointType.AnkleLeft),
                new Tuple<JointType, JointType>(JointType.AnkleLeft, JointType.FootLeft)
            };

            bodyPens = new Pen[bodyBrushes.Length];
            for (int i = 0; i < bodyBrushes.Length; i++)
            {
                bodyPens[i] = new Pen(bodyBrushes[i], 1f);
            }
        }


        public Form1()
        {
            InitializeComponent();

            kinect = KinectSensor.GetDefault();
            kinect.IsAvailableChanged += Kinect_IsAvailableChanged;
            InitializeColorComponents();

            InitializeDisplayComponents();

            InitializeBodyComponents();

            InitBones();

            gr.FillRectangle(Brushes.Black, pictureBox1.ClientRectangle);

            pictureBox1.Image = bmp;
            coordinateMapper = kinect.CoordinateMapper;
            kinect.Open();
        }

        void InitializeDisplayComponents()
        {
            var frameDesc = colorFrameSource.FrameDescription;
            displayHeight = Math.Min(frameDesc.Height, pictureBox1.Height);
            displayWidth = Math.Min(frameDesc.Width, pictureBox1.Width);
            bmp = new Bitmap(displayWidth, displayHeight);
            gr = Graphics.FromImage(bmp);
        }

        void InitializeColorComponents()
        {
            colorFrameSource = kinect.ColorFrameSource;
            colorFrameReader = colorFrameSource.OpenReader();
            colorFrameReader.FrameArrived += ColorReader_FrameArrived;
            colorWidth = colorFrameSource.FrameDescription.Width;
            colorHeight = colorFrameSource.FrameDescription.Height;
        }

        void InitializeBodyComponents()
        {
            bodyFrameSource = kinect.BodyFrameSource;

            bodyCount = bodyFrameSource.BodyCount;
            bodies = new Body[bodyCount];

            bodyFrameReader = bodyFrameSource.OpenReader();
            bodyFrameReader.FrameArrived += BodyReader_FrameArrived;
        }

        private void Kinect_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            statusLabel.Text = e.IsAvailable ? "" : "Kinect sensor not available!";
        }


        private void ColorReader_FrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            using (var frame = e.FrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    if (colorFrameBuffer == null)
                    {
                        colorFrameBuffer = new byte[frame.FrameDescription.Width * frame.FrameDescription.Height * 4];
                    }
                    frame.CopyConvertedFrameDataToArray(colorFrameBuffer, ColorImageFormat.Rgba);
                    lastColorFrameTime = DateTime.Now;
                }
            }
        }

        private void BodyReader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            bool dataReceived = false;

            using (var frame = e.FrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    if (bodies == null)
                    {
                        bodies = new Body[frame.BodyCount];
                    }

                    frame.GetAndRefreshBodyData(bodies);
                    dataReceived = true;
                }
            }


            if (dataReceived)
            {
                gr.FillRectangle(Brushes.DimGray, pictureBox1.ClientRectangle);

                for (int i = 0; i < bodies.Length; i++)
                {
                    Dictionary<JointType, ColorSpacePoint> jointColorSpacePoints =
                        new Dictionary<JointType, ColorSpacePoint>();

                    foreach (var jointType in bodies[i].Joints.Keys)
                    {
                        CameraSpacePoint cameraPoint = bodies[i].Joints[jointType].Position;
                        if (cameraPoint.Z < 0)
                        {
                            cameraPoint.Z = 0.1f;
                        }


                        ColorSpacePoint colorPoint =
                            coordinateMapper.MapCameraPointToColorSpace(bodies[i].Joints[jointType].Position);

                        jointColorSpacePoints.Add(jointType, colorPoint);
                        if (jointType == JointType.Head && colorPoint.X >= 0 && colorPoint.Y >= 0)
                        {
                            DrawColorBoxAroundPoint(colorPoint, (int) (300 / cameraPoint.Z), (int) (350 / cameraPoint.Z));
                        }

                        gr.FillEllipse(bodyBrushes[i], colorPoint.X * displayWidth / colorWidth,
                            colorPoint.Y * displayHeight / colorHeight, JointSize, JointSize);
                    }

                    foreach (var bone in bones)
                    {
                        DrawBone(bodies[i].Joints, jointColorSpacePoints, bone.Item1, bone.Item2, i);
                    }
                }
            }

            pictureBox1.Invalidate();
        }


        private void DrawColorBoxAroundPoint(ColorSpacePoint colorPoint, int boxWidth = 160, int boxHeight = 200)
        {
            if (colorFrameBuffer != null)
            {
                var x = (int) (colorPoint.X - boxWidth / 2);
                if (x < 0) x = 0;
                if (x > colorWidth) x = colorWidth;

                var y = (int) (colorPoint.Y - boxHeight * 6 / 11);
                if (y < 0) y = 0;
                if (y > colorHeight) y = colorHeight;

                var width = Math.Min(colorWidth - x, boxWidth);
                var height = Math.Min(colorHeight - y, boxHeight);

                var buffer = colorFrameBuffer;
                for (int i = 0; i < width; i++)
                {
                    for (int j = 0; j < height; j++)
                    {
                        int bufferAddr = 4 * ((y + j) * colorWidth + x + i);
                        // TODO: Interpolation?
                        Color pixelColor = Color.FromArgb(255, buffer[bufferAddr], buffer[bufferAddr + 1],
                            buffer[bufferAddr + 2]);
                        // TODO: Fast pixel data access via pointers
                        bmp.SetPixel((x + i) * displayWidth / colorWidth, (y + j) * displayHeight / colorHeight,
                            pixelColor);
                    }
                }
                statusLabel.Text = string.Format("FPS: {0:F2}",
                    (1000f / (DateTime.Now - lastColorFrameTime).Milliseconds));
            }
        }

        private void DrawBone(IReadOnlyDictionary<JointType, Joint> joints,
            IDictionary<JointType, ColorSpacePoint> jointCameraSpacePoints, JointType jointType0, JointType jointType1,
            int bodyIndex)
        {
            Joint joint0 = joints[jointType0];
            Joint joint1 = joints[jointType1];

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == TrackingState.NotTracked ||
                joint1.TrackingState == TrackingState.NotTracked)
            {
                return;
            }

            gr.DrawLine(bodyPens[bodyIndex], jointCameraSpacePoints[jointType0].X * displayWidth / colorWidth,
                jointCameraSpacePoints[jointType0].Y * displayHeight / colorHeight,
                jointCameraSpacePoints[jointType1].X * displayWidth / colorWidth,
                jointCameraSpacePoints[jointType1].Y * displayHeight / colorHeight);
        }


        private void button1_Click(object sender, EventArgs e)
        {
            if (kinect.IsOpen)
            {
                kinect.Close();
                button1.Text = "Start";
            }
            else
            {
                kinect.Open();
                button1.Text = "Stop";
            }
        }
    }
}