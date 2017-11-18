using System.Collections.Generic;

namespace Face
{
    public interface IFaceInfo<T>
    {
        float GetSimilarity(T template);
        void AddTemplate(T template);
        ICollection<T> Templates { get; }
        void Merge(IFaceInfo<T> other);
        bool IsValid(T template);
        IFaceInfo<T> NewInstance();
    }
}