using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace KinectUnifier
{
    public static class Util
    {
        public static IDictionary<JointType, Point2F> MapJointsToColorSpace(IBody body, ICoordinateMapper mapper)
        {
            var ret = new Dictionary<JointType, Point2F>();

            foreach (var joint in body.Joints)
            {
                Point3F cameraPoint = joint.Value.Position;
                if (cameraPoint.Z < 0)
                {
                    cameraPoint.Z = 0.1f;
                }

                Point2F colorPoint =
                       mapper.MapCameraPointToColorSpace(joint.Value.Position);

                ret.Add(joint.Key, colorPoint);
            }

            return ret;
        }

        /// <summary>
        /// The factor by which the distance from a body's neck to its head is multiplied to obtain the width of its face
        /// </summary>
        const float FaceWidth = 1.6f;
        /// <summary>
        /// The factor by which the distance from a body's neck to its neck is multiplied to obtain the height of its face
        /// </summary>
        const float FaceHeight = 2.1f;

        public static bool TryGetHeadRectangleAndRotAngle(IBody body, ICoordinateMapper mapper, out Rectangle faceRect, out double rotationAngle)
        {
            IJoint headJoint;
            IJoint neckJoint;

            faceRect = Rectangle.Empty;
            rotationAngle = 0;

            if (!body.Joints.TryGetValue(JointType.Head, out headJoint) ||
                !body.Joints.TryGetValue(JointType.Neck, out neckJoint)) return false;
            if (!headJoint.IsTracked || !neckJoint.IsTracked) return false;

            var headJointColorPos = mapper.MapCameraPointToColorSpace(headJoint.Position);
            var neckJointColorPos = mapper.MapCameraPointToColorSpace(neckJoint.Position);
            float headNeckDistance = headJointColorPos.DistanceTo(neckJointColorPos);
            rotationAngle = Math.Asin((headJointColorPos.X - neckJointColorPos.X) / headNeckDistance) * 180 / Math.PI;
            bool isFaceVertical = Math.Abs(rotationAngle) < 45;
            var width =  isFaceVertical ? headNeckDistance * FaceWidth : headNeckDistance * FaceHeight;
            var height = isFaceVertical ? headNeckDistance * FaceHeight : headNeckDistance * FaceWidth;
            faceRect =  new Rectangle(
                (int)(headJointColorPos.X - width / 2), 
                (int)(headJointColorPos.Y - height / 2), 
                (int)width, 
                (int)height);

            return true;
        }

        /// <summary>
        /// Get a rectangle containing a body's face based on the position of its neck and head
        /// </summary>
        /// <param name="body">The body of which we want the face rectangle</param>
        /// <param name="mapper">Coordinate mapper <see cref="IKinect.CoordinateMapper"/></param>
        /// <param name="faceRect">A <code>Rectangle</code> containing <code>body</code>'s face</param>
        /// <returns>True if succesful</returns>
        public static bool TryGetHeadRectangle(IBody body, ICoordinateMapper mapper, out Rectangle faceRect)
        {
            IJoint headJoint;
            IJoint neckJoint;

            faceRect = Rectangle.Empty;

            if (!body.Joints.TryGetValue(JointType.Head, out headJoint) ||
                !body.Joints.TryGetValue(JointType.Neck, out neckJoint)) return false;
            if (!headJoint.IsTracked || !neckJoint.IsTracked) return false;

            var headJointColorPos = mapper.MapCameraPointToColorSpace(headJoint.Position);
            var neckJointColorPos = mapper.MapCameraPointToColorSpace(neckJoint.Position);
            float headNeckDistance = headJointColorPos.DistanceTo(neckJointColorPos);

            bool isFaceVertical = Math.Abs(headJointColorPos.Y - neckJointColorPos.Y) >
                                  Math.Abs(headJointColorPos.X - neckJointColorPos.X);

            var width = isFaceVertical ? headNeckDistance * FaceWidth : headNeckDistance * FaceHeight;
            var height = isFaceVertical ? headNeckDistance * FaceHeight : headNeckDistance * FaceWidth;
            faceRect = new Rectangle(
                (int)(headJointColorPos.X - width / 2),
                (int)(headJointColorPos.Y - height / 2),
                (int)width,
                (int)height);

            return true;
        }

        public static Bitmap BytesToBitmap(this byte[] buffer, int width, int height, int bytesPerPixel)
        {
            if (buffer == null) return null;
            var bmp = new Bitmap(width, height, PixelFormat.Format32bppRgb);
            var bmpData = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);

            Marshal.Copy(buffer, 0, bmpData.Scan0, buffer.Length);
            
            bmp.UnlockBits(bmpData);
            return bmp;
        }
        
        public static Rectangle Rescale(this Rectangle origRect, int origWidth, int origHeight, int newWidth, int newHeight)
        {
            float xRatio = newWidth / (float)origWidth;
            float yRatio = newHeight / (float)origHeight;
            return new Rectangle((int)(origRect.X * xRatio), (int)(origRect.Y * yRatio), (int)(origRect.Width * xRatio), (int)(origRect.Height * yRatio));

        }

        public static Point Rescale(this Point origPoint, int origWidth, int origHeight, int newWidth, int newHeight)
        {
            float xRatio = newWidth / (float)origWidth;
            float yRatio = newHeight / (float)origHeight;
            return new Point((int)(origPoint.X * xRatio), (int)(origPoint.Y * yRatio));
        }

        /// <summary>
        /// Get a rectangular cutout from a pixel buffer
        /// </summary>
        /// <param name="buffer">The original pixel buffer</param>
        /// <param name="bufferWidth">Width of the image stored in <code>buffer</code></param>
        /// <param name="rect">The desired rectangle</param>
        /// <param name="bytesPerPixel">Bytes per pixel of the original (as well as returned) image</param>
        /// <param name="reverseRgb">If true, data in the copied buffer will turned from (A)RGB to BGR(A) and vice versa</param>
        /// <returns>A buffer containing image data in the region defined by <code>rect</code></returns>
        public static byte[] GetBufferRect(this byte[] buffer, int bufferWidth, Rectangle rect, int bytesPerPixel)
        {
            int left = rect.Left;
            if (left < 0) left = 0;

            int right = rect.Right;
            if (right > bufferWidth) right = bufferWidth;

            int top = rect.Top;
            if (top < 0) top = 0;

            int bufferHeight = buffer.Length / bytesPerPixel / bufferWidth;
            int bottom = rect.Bottom;
            if (bottom > bufferHeight) bottom = bufferHeight;

            int width = right - left;
            int height = bottom - top;

            var ret = new byte[width * height * bytesPerPixel];
            int targetI = 0;

            for (int y = top; y < bottom; y++)
            {
                for (int x = left; x < right; x++)
                {
                    var index = (y * bufferWidth + x) * bytesPerPixel;
                    for (int i = index; i < index + bytesPerPixel; i++)
                    {
                        ret[targetI++] = buffer[i];
                    }
                    
                }
            }

            return ret;
        }

        public static Rectangle TrimRectangle(this Rectangle rect, int width, int height)
        {
            int left = rect.Left;
            if (left < 0) left = 0;

            int right = rect.Right;
            if (right > width) right = width;

            int top = rect.Top;
            if (top < 0) top = 0;

            int bottom = rect.Bottom;
            if (bottom > height) bottom = height;

            return new Rectangle(left, top, right - left, bottom - top);
        }
    }


    public struct Point4F
    {
        public float X;
        public float Y;
        public float Z;
        public float W;

        public Point4F(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }
    }

    public struct Point3F
    {
        public float X;
        public float Y;
        public float Z;

        public Point3F(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static Point3F operator +(Point3F p1, Point3F p2)
        {
            return new Point3F(p1.X + p2.X, p1.Y + p2.Y, p1.Z + p1.Z);
        }

        public static Point3F operator /(Point3F p, int i)
        {
            return new Point3F(p.X  / i, p.Y / i, p.Z / i);
        }
    }

    public struct Point2F
    {
        public float X;
        public float Y;

        public Point2F(float x, float y)
        {
            X = x;
            Y = y;
        }

        public float DistanceTo(Point2F other)
        {
            return (float) Math.Sqrt((X - other.X) * (X - other.X) + (Y - other.Y) * (Y - other.Y));
        }
    }
}
