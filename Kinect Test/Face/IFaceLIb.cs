using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using KinectUnifier;

namespace Face
{
    public interface IFaceLIb
    {
        void FeedFrame(byte[] buffer);
        void FeedFacePositions(Rectangle[] facePositions);
        void FeedFacePositions(Rectangle[] facePositions, double[] rotationAngles);

        Rectangle[] GetFacePostions();
        int NumberOfFaces { get; }
        Point[] GetFacialFeatures(int faceIndex);
        byte[] GetFaceTemplate(int faceIndex);


        int FrameWidth { get; set; }
        int FrameHeight { get; set; }
        int FrameBytesPerPixel { get; set; }
    }
}
