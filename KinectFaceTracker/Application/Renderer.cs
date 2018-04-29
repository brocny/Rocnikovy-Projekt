using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Core;
using System.Numerics;

namespace App
{
    public class Renderer
    {
        public Font NameFont { get; set; }
        public StringFormat NameFormat { get; set; } = new StringFormat{ Alignment = StringAlignment.Center };

        public IList<Brush> BodyBrushes { get; set; } = new List<Brush>
            { Brushes.LimeGreen, Brushes.Blue, Brushes.Yellow, Brushes.Orange, Brushes.DeepPink, Brushes.Red};
        public IList<Pen> BodyPens { get; set; } 

        public Bitmap Image
        {
            get => _bmp;
            set
            {
                _bmp = value;
                _gr = Graphics.FromImage(_bmp);
            }
        }

        public Renderer(int colorFrameWidth, int colorFrameHeight)
        {
            _colorWidth = colorFrameWidth;
            _colorHeight = colorFrameHeight;
            NameFont = new Font(FontFamily.GenericSansSerif, _colorWidth / 60f);
            
            BodyPens = BodyBrushes.Select(br => new Pen(br, 2.5f)).ToArray();
            _bmp = new Bitmap(_colorWidth, _colorHeight);
        }
        
        private Bitmap _bmp;

        private readonly int _colorHeight;
        private readonly int _colorWidth;

        public float JointSize { get; set; } = 7;
        public float BoneThickness { get; set; } = 1;

        private Graphics _gr;

        public void Clear()
        {
            if (_bmp == null)
            {
                Image = new Bitmap(_colorWidth, _colorHeight);
            }
            else
            {
                _gr.Clear(Color.Black);
            }
        }

        public void DrawBody(IBody body, Brush brush, Pen pen, ICoordinateMapper mapper)
        {
            var jointColorSpacePoints = Util.MapJointsToColorSpace(body, mapper);
            foreach (var jointType in body.Joints.Keys)
            {
                DrawJoint(jointColorSpacePoints[jointType], brush);
            }

            foreach (var (joint1, joint2) in body.Bones)
            {
                DrawBone(body.Joints, jointColorSpacePoints, joint1, joint2, pen);
            }
        }

        public void DrawRectangles(Rectangle[] rects, long[] trackingIds = null)
        {
            if (trackingIds == null || trackingIds.Length != rects.Length)
            {
                for (int i = 0; i < rects.Length; i++)
                {
                    _gr.DrawRectangle(BodyPens[i % BodyPens.Count], rects[i]);
                }
            }
            else
            {
                for (int i = 0; i < rects.Length; i++)
                {
                    _gr.DrawRectangle(BodyPens[(int)trackingIds[i] % BodyPens.Count], rects[i]);
                }
            }

            
        }

        public void DrawBodies(IBody[] bodies, ICoordinateMapper mapper)
        {
            foreach (var body in bodies)
            {
                if (body.IsTracked)
                {
                    DrawBody(body, BodyBrushes[(int)(body.TrackingId % BodyBrushes.Count)], BodyPens[(int)(body.TrackingId % BodyPens.Count)], mapper);
                }
            }
        }

        public void DrawJoint(Vector2 pos, Brush brush)
        {
            var bmpX = Math.Max(0, Math.Min(pos.X, _bmp.Width));
            var bmpY = Math.Max(0, Math.Min(pos.Y, _bmp.Height));
            _gr.FillEllipse(brush, bmpX - JointSize / 2, bmpY - JointSize / 2, JointSize, JointSize);
        }

        public void DrawJoint(Vector2 pos, Brush brush, float size)
        {
            var bmpX = Math.Min(pos.X, _bmp.Width);
            var bmpY = Math.Min(pos.Y, _bmp.Height);
            _gr.FillEllipse(brush, bmpX - size / 2, bmpY - size / 2, size, size);
        }

        public void DrawBone(PointF p1, PointF p2, Pen pen)
        {
            _gr.DrawLine(pen, p1, p2);
        }

        public void DrawBone(IReadOnlyDictionary<JointType, IJoint> joints,
            IDictionary<JointType, Vector2> jointColorSpacePoints, JointType jointType0, JointType jointType1, Pen pen)
        {
            if (!joints.TryGetValue(jointType0, out var joint0) || !joints.TryGetValue(jointType1, out var joint1))
            {
                return;
            }

            if (!joint0.IsTracked || !joint1.IsTracked) return;

           
            _gr.DrawLine(pen,
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
            foreach (var f in features)
            {
                int bmpX = f.X;
                int bmpY = f.Y;

                _gr.FillEllipse(brush, bmpX, bmpY, size, size);
            }
        }

        public void DrawName(string name, int xPos, int yPos, Brush brush)
        {
            if (name != null)
                _gr.DrawString(name, NameFont, brush, xPos, yPos, NameFormat);
        }

        public void DrawName(string name, Point location, Brush brush)
        {
            DrawName(name, location.X, location.Y, brush);
        }

        public void DrawNames(string[] names, Point[] positions, long[] trackingIds = null)
        {
            if (trackingIds == null || trackingIds.Length != names.Length)
            {
                for (int i = 0; i < names.Length; i++)
                {
                    _gr.DrawString(names[i], NameFont, BodyBrushes[i % BodyBrushes.Count], positions[i]);
                }
            }
            else
            {
                for (int i = 0; i < names.Length; i++)
                {
                    _gr.DrawString(names[i], NameFont, BodyBrushes[(int)trackingIds[i] % BodyBrushes.Count], positions[i]);
                }
            }

            
        }

    }
}
