using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Kinect;
using Microsoft.Kinect.Face;

namespace Kinect_Test
{
    public partial class Form1 : Form
    {
        private Graphics gr;

        private const int JointSize = 5;

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


        private FaceFrameResult[] faceFrameResults;
        private FaceFrameSource[] faceFrameSources;
        private FaceFrameReader[] faceFrameReaders;

        private int displayWidth;
        private int displayHeight;
        
       
        

        public Form1()
        {
            
            InitializeComponent();
            // pictureBox1.Invalidate();


            kinect = KinectSensor.GetDefault();
            kinect.IsAvailableChanged += Kinect_IsAvailableChanged;
            InitializeColorComponents();

            InitializeDisplayComponents();

            InitializeBodyComponents();

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
        
        private void InitializeFaceComponents()
        {
            faceFrameResults = new FaceFrameResult[bodyCount];
            faceFrameReaders = new FaceFrameReader[bodyCount];
            faceFrameSources = new FaceFrameSource[bodyCount];
            for (int i = 0; i < bodyCount; i++)
            {
                faceFrameSources[i] = new FaceFrameSource(kinect, 0, FaceFrameFeatures.BoundingBoxInColorSpace);
                faceFrameReaders[i] = faceFrameSources[i].OpenReader();
                faceFrameReaders[i].FrameArrived += FaceReader_FrameArrived;
            }
        }

        private void Kinect_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            label1.Text = e.IsAvailable ? "" : "Kinect sensor not available!";
        }

        private void FaceReader_FrameArrived(object sender, FaceFrameArrivedEventArgs e)
        {
            using (var faceFrame = e.FrameReference.AcquireFrame())
            {
                var faceFrameResult = faceFrame?.FaceFrameResult;
                
                if (faceFrameResult != null)
                {
                    faceFrameResults[GetFaceSourceIndex(faceFrame.FaceFrameSource)] = faceFrameResult;
                }
            }
        }


        private int GetFaceSourceIndex(FaceFrameSource faceFrameSource)
        {
            for (int i = 0; i < faceFrameSources.Length; i++)
            {
                if (faceFrameSource == faceFrameSources[i])
                {
                    return i;
                }
            }
            return -1;
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
                   // label1.Text += frame.ColorCameraSettings.FrameInterval.Milliseconds.ToString() + "ms ";
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
                gr.FillRectangle(Brushes.DarkGray, pictureBox1.ClientRectangle);
                

                foreach (var body in bodies)
                {
                    foreach (var jointType in body.Joints.Keys)
                    {


                        CameraSpacePoint cameraPoint = body.Joints[jointType].Position;
                        if (cameraPoint.Z < 0)
                        {
                            cameraPoint.Z = 0.1f;
                        }
                        ColorSpacePoint colorPoint =
                            coordinateMapper.MapCameraPointToColorSpace(body.Joints[jointType].Position);
                        
                        if (jointType == JointType.Head && colorPoint.X >= 0 && colorPoint.Y >= 0)
                        {
                            DrawColorBoxAroundPoint(colorPoint);
                        }

                        gr.FillEllipse(Brushes.BlueViolet, colorPoint.X * displayWidth / colorWidth, colorPoint.Y * displayHeight / colorHeight, JointSize, JointSize);

                    }
                }

            }
            /*for (int i = 0; i < faceFrameResults.Length; i++)
            {
                if (faceFrameSources[i].IsTrackingIdValid)
                {
                    if (faceFrameResults[i] != null)
                    {
                        var faceRectI = faceFrameResults[i].FaceBoundingBoxInColorSpace;
                        var rect = pictureBox1.ClientRectangle;//RectIToRect(faceRectI);
                        var data = bmp.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppRgb);
                        
                        gr.FillRectangle(Brushes.Yellow, pictureBox1.ClientRectangle);

                        colorFrame?.CopyConvertedFrameDataToIntPtr(data.Scan0, (uint)(data.Width * data.Height * 4), ColorImageFormat.Rgba);
                         
                    }
                }
                else
                {
                    if (bodies[i] != null && bodies[i].IsTracked)
                    {
                        faceFrameSources[i].TrackingId = bodies[i].TrackingId;
                    }
                }
            }*/
            pictureBox1.Invalidate();
            
        }

        private Rectangle RectIToRect(RectI rectI)
        {
            return new Rectangle(
                rectI.Left * displayWidth / colorWidth,
                rectI.Top * displayHeight / colorHeight,
                -(rectI.Left - rectI.Right) * displayWidth / colorWidth,
                -(rectI.Top - rectI.Bottom) * displayHeight / colorHeight);
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void DrawColorBoxAroundPoint(ColorSpacePoint neckPoint)
        {

           
            if (colorFrameBuffer != null)
            {
                var x = (int)(neckPoint.X - 100);
                if (x < 0) x = 0;
                if (x > colorWidth) x = colorWidth;

                var y = (int)(neckPoint.Y - 150);
                if (y < 0) y = 0;
                if (y > colorHeight) y = colorHeight;

                var width = Math.Min(colorWidth - x, 200);
                var height = Math.Min(colorHeight - y, 250);

                var buffer = colorFrameBuffer;
                for (int i = 0; i < width; i++)
                {
                    for (int j = 0; j < height; j++)
                    {
                        int bufferAddr = 4 * ((y + j) * colorWidth + x + i);
                        bmp.SetPixel((x+i) * displayWidth / colorWidth, (y+j) * displayHeight / colorHeight, Color.FromArgb(255, buffer[bufferAddr], buffer[bufferAddr + 1], buffer[bufferAddr + 2] ));
                    }
                }
                label1.Text = (DateTime.Now - lastColorFrameTime).Milliseconds.ToString();
                pictureBox1.Invalidate();



                //bmp.UnlockBits(data);
            }
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
