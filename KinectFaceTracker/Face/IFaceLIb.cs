using System.Drawing;

namespace Face
{
    public interface IFaceLIb
    {
        void FeedFrame(byte[] buffer);
        void FeedFacePositions(Rectangle[] facePositions);
        void FeedFacePositions(Rectangle[] facePositions, double[] rotationAngles);

        Rectangle[] GetFacePositions();
        int NumberOfFaces { get; }
        Point[] GetFacialFeatures(int faceIndex);
        byte[] GetFaceTemplate(int faceIndex);


        int FrameWidth { get; set; }
        int FrameHeight { get; set; }
        int FrameBytesPerPixel { get; set; }
    }
}
