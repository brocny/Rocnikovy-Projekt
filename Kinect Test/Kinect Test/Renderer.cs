using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using KinectUnifier;

namespace Kinect_Test
{
    public class Renderer
    {
        public Font NameFont { get; set; } = new Font(FontFamily.GenericSansSerif, 20);

        public Bitmap Image
        {
            get { return _bmp; }
            set
            {
                _bmp = value;
            }
        }

        public Renderer(FormComponents components, int colorFrameWidth, int colorFrameHeight)
        {
            _colorWidth = colorFrameWidth;
            _colorHeight = colorFrameHeight;

            _bmp = new Bitmap(_colorWidth, _colorHeight);
        }
        
        private Bitmap _bmp;
        

        private readonly int _colorHeight;
        private readonly int _colorWidth;
        

        public float JointSize { get; set; } = 7;
        public float BoneThickness { get; set; } = 1;

        private DateTime _lastFrameTime;

        public void Clear()
        {
            var currentTime = DateTime.Now;
            _lastFrameTime = currentTime;
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

        public void DrawRectangles(Rectangle[] rects, Pen[] pens)
        {
            using (var gr = Graphics.FromImage(_bmp))
            {
                for (int i = 0; i < rects.Length; i++)
                {
                    gr.DrawRectangle(pens[i], rects[i]);
                }
            }
        }

        public void DrawBodyWithFaceBox(IBody body, byte[] colorBuffer, int colorFrameBpp, Brush brush, Pen pen, ICoordinateMapper mapper)
        {
            if (Util.TryGetHeadRectangleAndRotAngle(body, mapper, out var faceRect, out var _))
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
            using (var gr = Graphics.FromImage(_bmp))
            {
                var bmpX = Math.Min(pos.X, _bmp.Width);
                var bmpY = Math.Min(pos.Y, _bmp.Height);
                gr.FillEllipse(brush, bmpX - JointSize / 2, bmpY - JointSize / 2, JointSize, JointSize);
            }
        }

        public void DrawJoint(Point2F pos, Brush brush, float size)
        {
            using (var gr = Graphics.FromImage(_bmp))
            {
                var bmpX = Math.Min(pos.X, _bmp.Width);
                var bmpY = Math.Min(pos.Y, _bmp.Height);
                gr.FillEllipse(brush, bmpX - size / 2, bmpY - size / 2, size, size);
            }
        }

        public void DrawBone(PointF p1, PointF p2, Pen pen)
        {
            using(var gr = Graphics.FromImage(_bmp))
            gr.DrawLine(pen, p1, p2);
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
            using(var gr = Graphics.FromImage(_bmp))
            gr.DrawLine(pen,
                jointColorSpacePoints[jointType0].X,
                jointColorSpacePoints[jointType0].Y,
                jointColorSpacePoints[jointType1].X,
                jointColorSpacePoints[jointType1].Y
                );
        }

        public void DrawFacialFeatures(Point[] features, Brush brush, float size)
        {
            if (features == null)
                return;
            using(var gr = Graphics.FromImage(_bmp))
            foreach (var f in features)
            {
                var bmpX = f.X;
                var bmpY = f.Y;

                gr.FillEllipse(brush, bmpX, bmpY, size, size);
            }
        }

        public void DrawName(string name, int xPos, int yPos, Brush brush)
        {
            using (var gr = Graphics.FromImage(_bmp))
            {
                if (name != null)
                    gr.DrawString(name, NameFont, brush, xPos, yPos);
            }
        }

        public void DrawNames(string[] names, Point[] positions, Brush[] brushes)
        {
            using (var gr = Graphics.FromImage(_bmp))
            {
                for (int i = 0; i < names.Length; i++)
                {
                    gr.DrawString(names[i], NameFont, brushes[i], positions[i]);
                }
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

            var bmpX = x;
            var bmpY = y;
            var bmpWidth = rect.Width;
            var bmpHeight = rect.Height;

            if (bmpWidth == 0 || bmpHeight == 0 || bmpX + bmpWidth > _bmp.Width || bmpY + bmpHeight > _bmp.Height)
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
                    int bmpLineAddr = i * bmpData.Stride;
                    for (int j = 0; j < width; ++j)
                    {
                        var bmpAddr = bmpLineAddr + j * 4;
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
