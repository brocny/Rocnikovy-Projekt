using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Numerics;
using System.Runtime.InteropServices;
using Core.Kinect;
using Core.Properties;

namespace Core
{
    public static class Util
    {
        /// <summary>
        ///     The factor by which the distance from a body's neck to its head is multiplied to obtain the width of its face
        /// </summary>
        private static readonly float FaceWidthMultiplier = CoreSettings.Default.FaceWidthMultiplier;

        /// <summary>
        ///     The factor by which the distance from a body's neck to its neck is multiplied to obtain the height of its face
        /// </summary>
        private static readonly float FaceHeightMultiplier = CoreSettings.Default.FaceHeightMultiplier;

        public static IDictionary<JointType, Vector2> MapJointsToColorSpace(IBody body, ICoordinateMapper mapper)
        {
            var ret = new Dictionary<JointType, Vector2>();

            foreach (var joint in body.Joints)
            {
                var cameraPoint = joint.Value.Position;
                if (cameraPoint.Z < 0)
                {
                    cameraPoint.Z = 0.1f;
                }

                var colorPoint =
                    mapper.MapCameraPointToColorSpace(joint.Value.Position);

                ret.Add(joint.Key, colorPoint);
            }

            return ret;
        }

        public static bool TryGetHeadRectangleAndYawAngle(IBody body, ICoordinateMapper mapper, out Rectangle faceRect,
            out double rotationAngle)
        {
            faceRect = Rectangle.Empty;
            rotationAngle = 0;

            if (!body.Joints.TryGetValue(JointType.Head, out var headJoint) ||
                !body.Joints.TryGetValue(JointType.Neck, out var neckJoint)) return false;
            if (!headJoint.IsTracked || !neckJoint.IsTracked) return false;

            var headJointColorPos = mapper.MapCameraPointToColorSpace(headJoint.Position);
            var neckJointColorPos = mapper.MapCameraPointToColorSpace(neckJoint.Position);
            float headNeckDistance = headJointColorPos.DistanceFrom(neckJointColorPos);
            rotationAngle = Math.Asin((headJointColorPos.X - neckJointColorPos.X) / headNeckDistance) * 180 / Math.PI;
            bool isFaceVertical = Math.Abs(rotationAngle) < 45;
            float width = isFaceVertical ? headNeckDistance * FaceWidthMultiplier : headNeckDistance * FaceHeightMultiplier;
            float height = isFaceVertical ? headNeckDistance * FaceHeightMultiplier : headNeckDistance * FaceWidthMultiplier;
            faceRect = new Rectangle(
                (int) (headJointColorPos.X - width / 2),
                (int) (headJointColorPos.Y - height / 2),
                (int) width,
                (int) height);

            return true;
        }

        /// <summary>
        ///     Get a rectangle containing a body's face based on the position of its neck and head
        /// </summary>
        /// <param name="body">The body of which we want the face rectangle</param>
        /// <param name="mapper">A coordinate mapper, used for mapping body's joints' positions to color space</param>
        /// <param name="faceRect">A <c>Rectangle</c> containing <c>body</c>'s face</param>
        /// <returns>True if succesful</returns>
        public static bool TryGetHeadRectangle(IBody body, ICoordinateMapper mapper, out Rectangle faceRect)
        {
            faceRect = Rectangle.Empty;

            if (!body.Joints.TryGetValue(JointType.Head, out var headJoint) ||
                !body.Joints.TryGetValue(JointType.Neck, out var neckJoint)) return false;
            if (!headJoint.IsTracked || !neckJoint.IsTracked) return false;

            var headJointColorPos = mapper.MapCameraPointToColorSpace(headJoint.Position);
            var neckJointColorPos = mapper.MapCameraPointToColorSpace(neckJoint.Position);
            float headNeckDistance = headJointColorPos.DistanceFrom(neckJointColorPos);

            bool isFaceVertical = Math.Abs(headJointColorPos.Y - neckJointColorPos.Y) >
                                  Math.Abs(headJointColorPos.X - neckJointColorPos.X);

            float width = isFaceVertical ? headNeckDistance * FaceWidthMultiplier : headNeckDistance * FaceHeightMultiplier;
            float height = isFaceVertical ? headNeckDistance * FaceHeightMultiplier : headNeckDistance * FaceWidthMultiplier;
            faceRect = new Rectangle(
                (int) (headJointColorPos.X - width / 2),
                (int) (headJointColorPos.Y - height / 2),
                (int) width,
                (int) height);

            return true;
        }

        public static Bitmap BytesToBitmap(this byte[] buffer, int width, int height, int bytesPerPixel)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            PixelFormat pf = default;

            if (bytesPerPixel == 3)
                pf = PixelFormat.Format24bppRgb;
            else if (bytesPerPixel == 4)
                pf = PixelFormat.Format32bppRgb;
            else if (bytesPerPixel == 2)
                pf = PixelFormat.Format16bppRgb565;

            return buffer.BytesToBitmap(width, height, pf);
        }

        public static Bitmap BytesToBitmap(this byte[] buffer, int width, int height, PixelFormat pf)
        {
            if(buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            var bmp = new Bitmap(width, height, pf);
            var bmpData = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly,
                bmp.PixelFormat);

            Marshal.Copy(buffer, 0, bmpData.Scan0, width * height * pf.BytesPerPixel());

            bmp.UnlockBits(bmpData);
            return bmp;
        }


        public static int BytesPerPixel(this PixelFormat pf)
        {
            switch (pf)
            {
                case PixelFormat.Format16bppArgb1555: case PixelFormat.Format16bppGrayScale: case PixelFormat.Format16bppRgb565: case PixelFormat.Format16bppRgb555:
                    return 2;
                case PixelFormat.Format24bppRgb:
                    return 3;
                case PixelFormat.Format32bppArgb: case PixelFormat.Format32bppPArgb: case PixelFormat.Format32bppRgb:
                    return 4;
                default:
                    return 0;
            }
        }

        public static Rectangle Rescale(this Rectangle origRect, int origWidth, int origHeight, int newWidth,
            int newHeight)
        {
            float widthRatio = newWidth / (float) origWidth;
            float heightRatio = newHeight / (float) origHeight;
            return origRect.Rescale(widthRatio, heightRatio);
        }

        public static Rectangle Rescale(this Rectangle origRect, float widthRatio, float heightRatio)
        {
            return new Rectangle((int)(origRect.X * widthRatio), (int)(origRect.Y * heightRatio),
                (int)(origRect.Width * widthRatio), (int)(origRect.Height * heightRatio));
        }

        public static Point Rescale(this Point origPoint, int origWidth, int origHeight, int newWidth, int newHeight)
        {
            float widthRatio = newWidth / (float) origWidth;
            float heightRatio = newHeight / (float) origHeight;
            return origPoint.Rescale(widthRatio, heightRatio);
        }

        public static Point Rescale(this Point origPoint, float widthRatio, float heightRatio)
        {
            return new Point((int)(origPoint.X * widthRatio), (int)(origPoint.Y * heightRatio));
        }

        /// <summary>
        ///     Get a rectangular cutout from a pixel buffer
        /// </summary>
        /// <param name="buffer">The original pixel buffer</param>
        /// <param name="bufferWidth">Width of the image stored in <code>buffer</code></param>
        /// <param name="rect">The desired rectangle</param>
        /// <param name="bytesPerPixel">Bytes per pixel of the original (as well as returned) image</param>
        /// <returns>A buffer containing image data in the region defined by <code>rect</code></returns>
        public static byte[] GetBufferRect(this byte[] buffer, int bufferWidth, Rectangle rect, int bytesPerPixel)
        {
            if(buffer == null)
                throw new ArgumentNullException(nameof(buffer));

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
                    int index = (y * bufferWidth + x) * bytesPerPixel;
                    for (int i = index; i < index + bytesPerPixel; i++)
                    {
                        ret[targetI++] = buffer[i];
                    }
                }
            }

            return ret;
        }

        public static Rectangle TrimRectangle(this Rectangle rect, int width, int height, bool trimLeft = true, bool trimTop = true)
        {
            int left = rect.Left;
            if (trimLeft && left < 0) left = 0;

            int right = rect.Right;
            if (right > width) right = width;

            int top = rect.Top;
            if (trimTop && top < 0) top = 0;

            int bottom = rect.Bottom;
            if (bottom > height) bottom = height;

            return new Rectangle(left, top, right - left, bottom - top);
        }
    }

    public static class Vector2Extensions
    {
        public static float DistanceFrom(this Vector2 v, Vector2 w)
        {
            return (float) Math.Sqrt((v.X - w.X) * (v.X - w.X) + (v.Y - w.Y) * (v.Y - w.Y));
        }
    }
}