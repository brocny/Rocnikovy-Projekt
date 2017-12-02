using System.Collections.Generic;

namespace Face
{
    public interface IFaceInfo<T>
    {
        float GetSimilarity(T template);
        void AddTemplate(T template);
        void Merge(IFaceInfo<T> other);
        bool IsValid(T template);
        string Name { get; set; }
        ICollection<T> Templates { get; }

        IFaceInfo<T> NewInstance();
    }
}