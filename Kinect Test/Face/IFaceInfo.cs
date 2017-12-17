using System.Collections.Generic;
using System.IO;

namespace Face
{
    public interface IFaceInfo<T>
    {
        float GetSimilarity(T template);
        void AddTemplate(T template);
        void Merge(IFaceInfo<T> other);
        bool IsValid(T template);
        string Name { get; set; }
        IReadOnlyCollection<T> Templates { get; }
        void Serialize(Stream stream);
        IFaceInfo<T> Deserialize(Stream stream); 
        IFaceInfo<T> NewInstance();
    }
}