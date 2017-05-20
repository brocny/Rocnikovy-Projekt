using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace KinectUnifier
{
    public struct Point3
    {
        public int X;
        public int Y;
        public int Z;

        public Point3(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
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

    public struct Point2
    {
        public int X;
        public int Y;

        public Point2(int x, int y)
        {
            X = x;
            Y = y;
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
    }
}
