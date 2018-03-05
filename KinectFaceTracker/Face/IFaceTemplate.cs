namespace Face
{
    public interface IFaceTemplate<out T>
    {
        T Template { get; }
        float Age { get; }
        Gender Gender { get; }
        float GenderConfidence { get; }
        long TrackingId { get; }
    }
}