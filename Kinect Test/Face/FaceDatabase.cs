using System;
using System.Collections.Generic;
using System.Linq;

namespace Face
{
    public class LuxandFaceDatabase<T>
    {
        private Dictionary<string, IFaceInfo<T>> _storedFaces;

        public LuxandFaceDatabase()
        {
            _storedFaces = new Dictionary<string, IFaceInfo<T>>();
        }

        public IEnumerable<string> GetAllNames()
        {
            return _storedFaces.Keys;
        }

        /// <summary>
        /// Will do nothing if a face the same <code>name</code> is already in the database
        /// </summary>
        /// <param name="name"></param>
        /// <param name="info"></param>
        /// <returns><code>true</code> if successful</returns>
        public bool TryAddNewFace(string name, IFaceInfo<T> info)
        {
            if (_storedFaces.ContainsKey(name))
            {
                return false;
            }
            _storedFaces.Add(name, info);
            return true;
        }

        public bool TryAddNewFace(string name, T template)
        {
            var fInfo = Activator.CreateInstance<IFaceInfo<T>>();
            fInfo.Templates.Add(template);
       
            return fInfo.IsValid(template) && TryAddNewFace(name, fInfo);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="template"></param>
        /// <returns><code>name</code> of the best matching face and <code>confidence</code> value [0, 1]</returns>
        public (string name, float confidence) GetBestMatch(T template)
        {
            (string, float) outValue = (string.Empty, 0);

            var matches = _storedFaces.AsParallel().Select(x => (x.Key, x.Value.GetSimilarity(template))).AsEnumerable();
            foreach (var match in matches)
            {
                if (match.Item2 > outValue.Item2)
                {
                    outValue = match;
                }
            }
            return outValue;
        }

        /// <summary>
        /// Add another template to existing face -- for example a different angle, with/out glasses, ...
        /// </summary>
        /// <param name="name"></param>
        /// <param name="faceTemplate"></param>
        /// <returns><code>true</code>if succesful</returns>
        /// <exception cref="ArgumentException"> thrown if <code>faceTemplate</code> has incorrect length</exception>
        public bool TryAddFaceTemplateToExistingFace(string name, T faceTemplate)
        {
            if (_storedFaces.TryGetValue(name, out var faceInfo))
            {
                faceInfo.AddTemplate(faceTemplate);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Merge face with the name <code>name2</code> into the face with the name <code>name1</code>
        /// </summary>
        /// <param name="name1"></param>
        /// <param name="name2"></param>
        /// <returns>True if succesful</returns>
        public bool Merge(string name1, string name2)
        {
            if (_storedFaces.TryGetValue(name1, out var info1) && _storedFaces.TryGetValue(name2, out var info2))
            {
                info2.Merge(info1);
                _storedFaces.Remove(name1);
                return true;
            }

            return false;
        }
    }
}