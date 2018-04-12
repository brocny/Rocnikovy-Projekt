using System.Drawing;
using KinectUnifier;

namespace Face
{
    public interface IFaceTemplate<out T>
    {
        T Template { get; }
        ImmutableImage FaceImage { get; }
        float Age { get; }
        Gender Gender { get; }
        float GenderConfidence { get; }
        long TrackingId { get; }
    }
}