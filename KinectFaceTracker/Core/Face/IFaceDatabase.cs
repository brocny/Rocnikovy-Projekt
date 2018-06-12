using System.Collections.Generic;
using System.IO;

namespace Core.Face
{
    public interface IFaceDatabase<T>
    {
        bool TryGetValue(int id, out IFaceInfo<T> faceInfo);
        /// <param name="faceId">Id of the desired face</param>
        /// <returns><see cref="IFaceInfo{T}"/> of the face with matching <param name="faceId"></param>, <c>null</c> if not present</returns>
        IFaceInfo<T> this[int faceId] { get; }
        IEnumerable<int> Keys { get; }
        IEnumerable<IFaceInfo<T>> Values { get; }
        IEnumerable<KeyValuePair<int, IFaceInfo<T>>> Pairs { get; }

        /// <summary>
        /// The next free available face ID
        /// </summary>
        int NextId { get; }
        bool ContainsKey(int key);
        bool TryAddNewFace(int id, IFaceInfo<T> faceInfo);
        Match<T> GetBestMatch(IFaceTemplate<T> template);
        Match<T> GetBestMatch(T template);

        /// <summary>
        /// Adds <paramref name="template"/> to face with <paramref name="id"/>, or adds a new face with the given id, if not present
        /// </summary>
        /// <param name="id"></param>
        /// <param name="template"></param>
        void AddOrUpdate(int id, IFaceTemplate<T> template);
        
        /// <summary>
        /// <see cref="AddOrUpdate(int, Core.Face.IFaceTemplate{T})"/>
        /// </summary>
        /// <param name="id"></param>
        /// <param name="template"></param>
        void AddOrUpdate(int id, T template);

        /// <summary>
        /// 
        /// </summary>
        void Clear();
        bool MergeFaces(int id1, int id2);

        /// <summary>
        /// Create a copy of the database, which can be restored using <seealso cref="Restore"/>
        /// </summary>
        /// <returns>A restorable copy of the database</returns>
        IFaceDatabase<T> Backup();

        /// <summary>
        /// Restore the database from a copy 
        /// </summary>
        /// <param name="backup">Backup to restore from</param>
        void Restore(IFaceDatabase<T> backup);

        /// <summary>
        /// Serialize the database into a stream
        /// </summary>
        /// <param name="stream">Stream to serialize into</param>
        void Serialize(Stream stream);

        /// <summary>
        /// Deserialize the database from a stream
        /// </summary>
        /// <param name="stream">Stream to deserilize from</param>
        void Deserialize(Stream stream);
    }
}
