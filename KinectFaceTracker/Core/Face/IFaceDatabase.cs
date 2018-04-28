using System.Collections.Generic;
using System.IO;

namespace Core.Face
{
    public interface IFaceDatabase<T>
    {
        string GetName(int id);
        bool TryGetValue(int id, out IFaceInfo<T> faceInfo);
        IFaceInfo<T> this[int index] { get; }
        IEnumerable<int> Keys { get; }
        IEnumerable<IFaceInfo<T>> Values { get; }
        IEnumerable<KeyValuePair<int, IFaceInfo<T>>> Pairs { get; }
        int NextId { get; }
        bool ContainsKey(int key);
        bool TryAddNewFace(int id, IFaceInfo<T> faceInfo);
        Match<T> GetBestMatch(IFaceTemplate<T> template);
        Match<T> GetBestMatch(T template);
        void AddOrUpdate(int id, IFaceTemplate<T> template);
        void AddOrUpdate(int id, T template);
        void Clear();
        bool MergeFaces(int id1, int id2);
        IFaceDatabase<T> Backup();
        void Restore(IFaceDatabase<T> backup);
        void Serialize(Stream stream);
        void Deserialize(Stream stream);
    }
}
