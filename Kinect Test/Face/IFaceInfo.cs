using System.Collections.Generic;
using System.IO;

namespace Face
{
    public interface IFaceInfo<T>
    {
        /// <summary>
        /// Get similarity between <paramref name="template"/> and <c>this</c>
        /// </summary>
        /// <param name="template">The template to compare</param>
        /// <returns>A value between 0 (no similaraty) and 1 (maximum similarity) between <c>this</c> and <c>template</c></returns>
        float GetSimilarity(T template);
        /// <summary>
        /// Add a new template to this face
        /// </summary>
        /// <param name="template">Template to be added</param>
        void AddTemplate(T template);
        /// <summary>
        /// Add all the templates in <paramref name="other"/> into this
        /// </summary>
        /// <param name="other"></param>
        void Merge(IFaceInfo<T> other);
        /// <summary>
        /// Checks whether <paramref name="template"/> is a valid face template
        /// </summary>
        /// <param name="template">Face template</param>
        /// <returns><c>true</c> if <paramref name="template"/> is a valid face template</returns>
        bool IsValid(T template);
        /// <summary>
        /// Identification of the face
        /// </summary>
        string Name { get; set; }
        IReadOnlyCollection<T> Templates { get; }
        /// <summary>
        /// Serialize this into a <c>Stream</c>
        /// </summary>
        /// <param name="stream"><c>Stream</c> to serialize into</param>
        void Serialize(Stream stream);
        /// <summary>
        /// Create a new instance of the same type from a <c>Stream</c>
        /// </summary>
        /// <param name="stream"><c>Stream</c> to deserialize from</param>
        /// <returns>A new instance</returns>
        IFaceInfo<T> Deserialize(Stream stream);
        /// <summary>
        /// Get a new instance of the same type
        /// </summary>
        /// <returns>A new instance of the same type</returns>
        IFaceInfo<T> NewInstance();
    }
}