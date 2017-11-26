using System;
using System.Collections.Generic;
using System.Linq;

namespace Face
{
    public class FaceDatabase<T>
    {
        private Dictionary<string, IFaceInfo<T>> _storedFaces = new Dictionary<string, IFaceInfo<T>>();
        private IFaceInfo<T> _baseInstance;

        public IEnumerable<string> GetAllNames()
        {
            return _storedFaces.Keys;
        }

        public FaceDatabase(IFaceInfo<T> baseInstance)
        {
            _baseInstance = baseInstance;
        }

        public FaceDatabase()
        {
            _baseInstance = Activator.CreateInstance<IFaceInfo<T>>();
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
            var faceInfo = _baseInstance.NewInstance();
       
            return faceInfo.IsValid(template) && TryAddNewFace(name, faceInfo);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="template"></param>
        /// <returns><code>name</code> of the best matching face and <code>confidence</code> value [0, 1]</returns>
        public (string name, float confidence) GetBestMatch(T template)
        {
            (string, float) retValue = (string.Empty, 0);

            var matches = _storedFaces.AsParallel().Select(x => (x.Key, x.Value.GetSimilarity(template))).AsEnumerable();
            foreach (var match in matches)
            {
                if (match.Item2 > retValue.Item2)
                {
                    retValue = match;
                }
            }
            return retValue;
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

        public void Add(string name, T faceTemlate)
        {
            if (_storedFaces.TryGetValue(name, out var faceInfo))
            {
                faceInfo.AddTemplate(faceTemlate);
            }
            else
            {
                var newInfo = _baseInstance.NewInstance();
                newInfo.AddTemplate(faceTemlate);
                if (newInfo.IsValid(faceTemlate))
                {
                    _storedFaces.Add(name, newInfo);
                }
                else
                {
                    throw new ArgumentException($"{nameof(faceTemlate)} invalid!");
                }
            }
        }

        public void Add(string name, IFaceInfo<T> template)
        {
            if (_storedFaces.TryGetValue(name, out var faceInfo))
            {
                faceInfo.Merge(template);
            }
            else
            {
                _storedFaces.Add(name, template);
            }
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