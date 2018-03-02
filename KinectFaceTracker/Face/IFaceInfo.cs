using System.Collections.Generic;
using System.IO;

namespace Face
{
    /// <summary>
    /// Interface for types used for storing multiple face templates of a single face
    /// </summary>
    /// <typeparam name="T">The type of face templates stored</typeparam>
    public interface IFaceInfo<T>
    {
        /// <summary>
        /// Get similarity between <paramref name="template"/> and <c>this</c>
        /// </summary>
        /// <param name="template">The template to compare</param>
        /// <returns>A value between 0 (no similaraty) and 1 (maximum similarity) between <c>this</c> and <paramref name="template"/></returns>
        float GetSimilarity(T template);

        /// <summary>
        /// Get similarity between <paramref name="faceInfo"/> and <c>this</c>
        /// </summary>
        /// <param name="faceInfo"></param>
        /// <returns>A value between 0 (no similaraty) and 1 (maximum similarity) between <c>this</c> and <paramref name="faceInfo"/></returns>
        float GetSimilarity(IFaceInfo<T> faceInfo);
        
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

        /// <summary>
        /// Estimated age of the person
        /// </summary>
        float? Age { get; set; }

        /// <summary>
        /// Person's gender
        /// </summary>
        Gender Gender { get; set; }

        /// <summary>
        /// Confidence that the <see cref="Gender"/> is correct between 0 and 1
        /// </summary>
        float? GenderConfidence { get; set; }

        IReadOnlyCollection<T> Templates { get; }
        
        /// <summary>
        /// Serialize this into a <c>Stream</c>
        /// </summary>
        /// <param name="stream"><c>Stream</c> to serialize into</param>
        void Serialize(Stream stream);

        void Serialize(TextWriter writer);
        
        /// <summary>
        /// Create a new instance of the same type from a <c>Stream</c>
        /// </summary>
        /// <param name="stream"><c>Stream</c> to deserialize from</param>
        /// <returns>A new instance</returns>
        IFaceInfo<T> Deserialize(Stream stream);

        IFaceInfo<T> Deserialize(TextReader reader);
            /// <summary>
        /// Get a new instance of the same type
        /// </summary>
        /// <returns>A new instance of the same type</returns>
        IFaceInfo<T> NewInstance();
    }

    public enum Gender
    {
        Uknown, Female, Male
    }
}