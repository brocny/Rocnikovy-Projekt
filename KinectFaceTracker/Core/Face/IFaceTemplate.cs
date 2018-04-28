using Core;

namespace Core.Face
{
    public interface IFaceTemplate<out T>
    {
        T Template { get; }
        ImageBuffer FaceImage { get; }
        float Age { get; }
        Gender Gender { get; }
        float GenderConfidence { get; }
        long TrackingId { get; }
    }
}