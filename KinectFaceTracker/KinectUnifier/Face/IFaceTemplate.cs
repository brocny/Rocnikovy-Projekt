using KinectUnifier;

namespace Face
{
    public interface IFaceTemplate<out T>
    {
        T Template { get; }
        ImageBuffer FaceImageBuffer { get; }
        float Age { get; }
        Gender Gender { get; }
        float GenderConfidence { get; }
        long TrackingId { get; }
    }
}