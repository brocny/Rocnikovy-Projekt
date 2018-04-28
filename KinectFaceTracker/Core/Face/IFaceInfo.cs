using System.Collections.Generic;
using System.IO;

namespace Core.Face
{
    /// <summary>
    ///     Interface for types used for storing multiple face templates of a single face
    /// </summary>
    /// <typeparam name="T">The type of face templates stored</typeparam>
    public interface IFaceInfo<T>
    {
        /// <summary>
        ///     Identification of the face
        /// </summary>
        string Name { get; set; }

        /// <summary>
        ///     Estimated age of the person
        /// </summary>
        float Age { get; }

        /// <summary>
        ///     Person's gender
        /// </summary>
        Gender Gender { get; }

        /// <summary>
        ///     Similarity that the <see cref="Gender" /> is correct between 0 and 1
        /// </summary>
        float GenderConfidence { get; }

        IEnumerable<FaceSnapshot<T>> Snapshots { get; }

        IEnumerable<T> Templates { get; }

        IEnumerable<ImageBuffer> Images { get; }

        /// <summary>
        ///     Get similarity between <paramref name="template" /> and <c>this</c>
        /// </summary>
        /// <param name="template">The template to compare</param>
        /// <returns>A value between 0 (no similaraty) and 1 (maximum similarity)</returns>
        (float similarity, FaceSnapshot<T> snapshot) GetSimilarity(T template);

        /// <summary>
        ///     Get similarity between <paramref name="faceInfo" /> and <c>this</c>
        /// </summary>
        /// <param name="faceInfo"></param>
        /// <returns>A value between 0 (no similarity) and 1 (maximum similarity)</returns>
        (float similarity, FaceSnapshot<T> snapshot) GetSimilarity(IFaceInfo<T> faceInfo);

        /// <summary>
        ///     Get similarity between
        ///     <param name="faceTemplate"> and <c>this</c></param>
        /// </summary>
        /// <param name="faceTemplate"></param>
        /// <returns>A value between 0 (no similarity) and 1 (maximum similarity)</returns>
        (float similarity, FaceSnapshot<T> snapshot) GetSimilarity(IFaceTemplate<T> faceTemplate);

        /// <summary>
        ///     Add a new template to this face
        /// </summary>
        /// <param name="template">Template to be added</param>
        /// <param name="imageBuffer"> Optionally add an image, from which the template was generated </param>
        void AddTemplate(T template, ImageBuffer imageBuffer = null);

        /// <summary>
        ///     Add a new template including additional information included in FaceTemplate<typeparamref name="T" /> besides the
        ///     actual template
        /// </summary>
        /// <param name="faceTemplate">Template to be added</param>
        void AddTemplate(IFaceTemplate<T> faceTemplate);

        /// <summary>
        ///     Add all the templates in <paramref name="other" /> into this
        /// </summary>
        /// <param name="other"></param>
        void Merge(IFaceInfo<T> other);

        /// <summary>
        ///     Checks whether <paramref name="template" /> is a valid face template
        /// </summary>
        /// <param name="template">Face template</param>
        /// <returns><c>true</c> if <paramref name="template" /> is a valid face template</returns>
        bool IsValid(T template);

        /// <summary>
        ///     Serialize this into a <c>Stream</c>
        /// </summary>
        /// <param name="stream"><c>Stream</c> to serialize into</param>
        void Serialize(Stream stream);

        /// <summary>
        ///     Serialize this using a <c>TextWriter</c>
        /// </summary>
        /// <param name="writer"><c>TextWriter</c> to write to for serialization</param>
        void Serialize(TextWriter writer);

        /// <summary>
        ///     Create a new instance of the same type from a <c>Stream</c>
        /// </summary>
        /// <param name="stream"><c>Stream</c> to deserialize from</param>
        /// <returns>A new instance</returns>
        IFaceInfo<T> Deserialize(Stream stream);

        /// <summary>
        ///     Create a new instance of the same type by reading from a <c>TextReader</c>
        /// </summary>
        /// <param name="reader"><c>TextReader</c> to read from</param>
        /// <returns>A new instance</returns>
        IFaceInfo<T> Deserialize(TextReader reader);

        /// <summary>
        ///     Get a new instance of the same type
        /// </summary>
        /// <returns>A new instance of the same type</returns>
        IFaceInfo<T> NewInstance();
    }

    public enum Gender
    {
        Unknown,
        Female,
        Male
    }
}