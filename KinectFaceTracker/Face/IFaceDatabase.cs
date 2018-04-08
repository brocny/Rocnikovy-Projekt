using System.Collections.Generic;

namespace Face
{
    public interface IFaceDatabase<T>
    {
        string GetName(int id);
        bool TryGetValue(int id, out IFaceInfo<T> faceInfo);
        IFaceInfo<T> this[int index] { get; }
        IEnumerable<int> Keys { get; }
        int NextId { get; }
        bool ContainsKey(int key);
        bool TryAddNewFace(int id, IFaceInfo<T> faceInfo);
        (int id, float confidence) GetBestMatch(IFaceTemplate<T> template);
        (int id, float confidence) GetBestMatch(T template);
        void AddOrUpdate(int id, IFaceTemplate<T> template);
        void AddOrUpdate(int id, T template);
        bool MergeFaces(int id1, int id2);
    }
}
