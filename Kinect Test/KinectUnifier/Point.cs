using System;

namespace KinectUnifier
{
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

        public static Point3F operator -(Point3F p1, Point3F p2)
        {
            return new Point3F(p1.X - p2.X, p1.Y - p2.Y, p1.Z - p2.Z);
        }

        public static Point3F operator /(Point3F p, int i)
        {
            return new Point3F(p.X / i, p.Y / i, p.Z / i);
        }

        public static implicit operator Point4F(Point3F point3F)
        {
            return new Point4F(point3F.X, point3F.Y, point3F.Z, 0);
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
            return (float)Math.Sqrt((X - other.X) * (X - other.X) + (Y - other.Y) * (Y - other.Y));
        }

        public static Point2F operator /(Point2F p, int i)
        {
            return new Point2F(p.X / i, p.Y);
        }

        public static implicit operator Point3F(Point2F point2F)
        {
            return new Point3F(point2F.X, point2F.Y, 0);
        }

        public static Point2F operator +(Point2F p1, Point2F p2)
        {
            return new Point2F(p1.X + p2.X, p1.Y + p2.Y);
        }

        public static Point2F operator -(Point2F p1, Point2F p2)
        {
            return new Point2F(p1.X - p2.X, p1.Y - p2.Y);
        }
    }
}