using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
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

        public static bool TryGetHeadRectangleInColorSpace(IBody body, ICoordinateMapper mapper, out Rectangle faceRect, out double rotationAngle)
        {
            IJoint headJoint;
            IJoint neckJoint;

            faceRect = Rectangle.Empty;
            rotationAngle = 0;

            if (!body.Joints.TryGetValue(JointType.Head, out headJoint) ||
                !body.Joints.TryGetValue(JointType.Neck, out neckJoint)) return false;
            if (!headJoint.IsTracked || !neckJoint.IsTracked) return false;

            var headJointCameraPos = mapper.MapCameraPointToColorSpace(headJoint.Position);
            var neckJointCameraPos = mapper.MapCameraPointToColorSpace(neckJoint.Position);
            float headNeckDistance = headJointCameraPos.DistanceTo(neckJointCameraPos);
            bool isFaceVertical = Math.Abs(headJointCameraPos.Y - neckJointCameraPos.Y) >
                                  Math.Abs(headJointCameraPos.X - neckJointCameraPos.X);
            var width =  isFaceVertical ? headNeckDistance * 1.6f : headNeckDistance * 1.9f;
            var height = isFaceVertical ? headNeckDistance * 1.9f : headNeckDistance * 1.6f;

            rotationAngle = Math.Asin( (headJointCameraPos.X - neckJointCameraPos.X) / headNeckDistance);

            faceRect =  new Rectangle(
                (int)(headJointCameraPos.X - width / 2), 
                (int)(headJointCameraPos.Y - height / 2.2f), 
                (int)width, 
                (int)height);

            return true;
        }

        public static Bitmap BytesToBitmap(byte[] buffer, int width, int height, int bytesPerPixel)
        {
            if (buffer == null) return null;
            var bmp = new Bitmap(width, height);
            var bmpData = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite,
                PixelFormat.Format32bppRgb);
            Marshal.Copy(buffer, 0, bmpData.Scan0, width * height * bytesPerPixel);
            bmp.UnlockBits(bmpData);
            return bmp;
        }
        
        public static Rectangle CoordinateSystemConversion(Rectangle origRect, int origWidth, int origHeight, int newWidth, int newHeight)
        {
            float xRatio = newWidth / (float)origWidth;
            float yRatio = newHeight / (float)origHeight;
            return new Rectangle((int)(origRect.X * xRatio), (int)(origRect.Y * yRatio), (int)(origRect.Width * xRatio), (int)(origRect.Height * yRatio));

        }

        public static Point CoordinateSystemConversion(Point origPoint, int origWidth, int origHeight, int newWidth, int newHeight)
        {
            float xRatio = newWidth / (float)origWidth;
            float yRatio = newHeight / (float)origHeight;
            return new Point((int)(origPoint.X * xRatio), (int)(origPoint.Y * yRatio));
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
