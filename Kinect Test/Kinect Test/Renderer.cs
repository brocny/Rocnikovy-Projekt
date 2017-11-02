using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using KinectUnifier;

namespace Kinect_Test
{
    public class Renderer
    {
        public Renderer(FormComponents components, int colorFrameWidth, int colorFrameHeight)
        {
            _components = components;

            _displayWidth = _components.PictureBox.Width;
            _displayHeight = _components.PictureBox.Height;

            _colorWidth = colorFrameWidth;
            _colorHeight = colorFrameHeight;

            _widthRatio = (float)_displayWidth / _colorWidth;
            _heightRatio = (float)_displayHeight / _colorHeight;

            _bmp = new Bitmap(_displayWidth, _displayHeight);
            _gr = Graphics.FromImage(_bmp);
            _components.PictureBox.Image = _bmp;
        }

        private FormComponents _components;
        private Graphics _gr;
        private Bitmap _bmp;

        private readonly int _displayHeight;
        private readonly int _displayWidth;

        private readonly int _colorHeight;
        private readonly int _colorWidth;

        private readonly float _heightRatio;
        private readonly float _widthRatio;

        public float JointSize { get; set; } = 7;
        public float BoneThickness { get; set; } = 1;

        public void ClearScreen()
        {
            _gr.FillRectangle(Brushes.Black, _components.PictureBox.ClientRectangle);
        }

        public void DrawBody(IBody body, Brush brush, Pen pen, ICoordinateMapper mapper)
        {
            var jointColorSpacePoints = Util.MapJointsToColorSpace(body, mapper);
            foreach (var jointType in body.Joints.Keys)
            {
                DrawJoint(jointColorSpacePoints[jointType], brush);
            }

            foreach (var bone in body.Bones)
            {
                DrawBone(body.Joints, jointColorSpacePoints, bone.Item1, bone.Item2, pen);
            }
        }

        public void DrawBodyWithFaceBox(IBody body, byte[] colorBuffer, int colorFrameBpp, Brush brush, Pen pen, ICoordinateMapper mapper)
        {
            if (Util.TryGetHeadRectangleInColorSpace(body, mapper, out var faceRect, out var _))
            {
                DrawColorBox(faceRect, colorBuffer, colorFrameBpp);
            }

            DrawBody(body, brush, pen, mapper);
        }

        public void DrawBodiesWithFaceBoxes(IBody[] bodies, byte[] colorBuffer, int colorFrameBpp, Brush[] brushes, Pen[] pens, ICoordinateMapper mapper)
        {
            for (int i = 0; i < bodies.Length; i++)
            {
                DrawBodyWithFaceBox(bodies[i], colorBuffer, colorFrameBpp, brushes[i% brushes.Length], pens[i % pens.Length], mapper);
            }
        }

        public void DrawBodies(IBody[] bodies, Brush[] brushes, Pen[] pens, ICoordinateMapper mapper)
        {
            for (int i = 0; i < bodies.Length; i++)
            {
                DrawBody(bodies[i], brushes[i % brushes.Length], pens[i % pens.Length], mapper);
            }
        }

        public void DrawJoint(Point2F pos, Brush brush)
        {
            var bmpX = pos.X * _widthRatio;
            var bmpY = pos.Y * _heightRatio;
            _gr.FillEllipse(brush, bmpX - JointSize / 2, bmpY - JointSize / 2, JointSize, JointSize);
        }

        public void DrawJoint(Point2F pos, Brush brush, float size)
        {
            var bmpX = pos.X * _widthRatio;
            var bmpY = pos.Y * _heightRatio;
            _gr.FillEllipse(brush, bmpX - size / 2, bmpY - size / 2, size, size);
        }

        public void DrawBone(PointF p1, PointF p2, Pen pen)
        {
            _gr.DrawLine(pen, p1, p2);
        }

        public void DrawBone(IReadOnlyDictionary<JointType, IJoint> joints,
            IDictionary<JointType, Point2F> jointColorSpacePoints, JointType jointType0, JointType jointType1, Pen pen)
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
                //return;
            }

            _gr.DrawLine(pen,
                jointColorSpacePoints[jointType0].X * _widthRatio,
                jointColorSpacePoints[jointType0].Y * _heightRatio,
                jointColorSpacePoints[jointType1].X * _widthRatio,
                jointColorSpacePoints[jointType1].Y * _heightRatio
                );
        }

        public void DrawFacialFeatures(Point[] features, Brush brush, float size)
        {
            if (features == null)
                return;

            foreach (var f in features)
            {
                var bmpX = f.X * _widthRatio;
                var bmpY = f.Y * _heightRatio;

                _gr.FillEllipse(brush, bmpX, bmpY, size, size);
            }
        }

        public void DrawColorBox(Rectangle rect, byte[] colorFrameBuffer, int colorFrameBpp)
        {
            if (rect.Width == 0 || rect.Height == 0 || colorFrameBuffer == null)
                return;

            var x = rect.X;
            if (x < 0) x = 0;
            if (x > _colorWidth) x = _colorWidth;

            var y = rect.Y;
            if (y < 0) y = 0;
            if (y > _colorWidth) y = _colorWidth;

            var height = rect.Height;
            var width = rect.Width;
            if (x + width > _colorWidth) width = _colorWidth - x;
            if (y + height > _colorHeight) height = _colorHeight - y;

            var buffer = colorFrameBuffer;

            var bmpX = (int)(x * _widthRatio);
            var bmpY = (int)(y * _heightRatio);
            var bmpWidth = (int)(rect.Width * _widthRatio);
            var bmpHeight = (int)(rect.Height * _heightRatio);

            if (bmpWidth == 0 || bmpHeight == 0)
                return;

            var bmpData = _bmp.LockBits(new Rectangle(bmpX, bmpY, bmpWidth, bmpHeight),
                ImageLockMode.WriteOnly,
                _bmp.PixelFormat);

            unsafe
            {
                byte* bmpPointer = (byte*)bmpData.Scan0;
                for (int i = 0; i < height; ++i)
                {
                    int bufAddr = colorFrameBpp * ((y + i) * _colorWidth + x);
                    int bmpLineAddr = i * _displayHeight / _colorHeight * bmpData.Stride;
                    for (int j = 0; j < width; ++j)
                    {
                        var bmpAddr = bmpLineAddr + j * _displayWidth / _colorWidth * 4;
                        bmpPointer[bmpAddr + 2] = buffer[bufAddr];
                        bmpPointer[bmpAddr + 1] = buffer[bufAddr + 1];
                        bmpPointer[bmpAddr] = buffer[bufAddr + 2];

                        bufAddr += colorFrameBpp;
                    }
                }
            }
            _bmp.UnlockBits(bmpData);
        }

    }


    
    
}
