using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Face
{
    /// <summary>
    /// A database for storing information about faces
    /// </summary>
    /// <remarks>
    /// Thread-safe 
    /// </remarks>
    /// <typeparam name="T">Type of face templates</typeparam>
    public partial class FaceDatabase<T> : ICloneable, IFaceDatabase<T>
    {
        private ConcurrentDictionary<int, IFaceInfo<T>> _storedFaces;
        private IFaceInfo<T> _baseInstance;

        public int NextId { get; private set; }
        public string SerializePath { get; set; } = null;

        public FaceDatabase(IFaceInfo<T> baseInstance = null, IEnumerable<KeyValuePair<int, IFaceInfo<T>>> initialDb = null)
        {
            if (baseInstance == null)
            {
                var firstType = (from t in Assembly.GetExecutingAssembly().GetExportedTypes()
                        where !t.IsAbstract && !t.IsInterface && typeof(IFaceInfo<T>).IsAssignableFrom(t) && t.GetConstructor(Type.EmptyTypes) != null
                        select t)
                    .FirstOrDefault();

                if (firstType != null)
                {
                    _baseInstance = (IFaceInfo<T>)Activator.CreateInstance(firstType);
                }
                else
                {
                    throw new ApplicationException($"No suitable class implementing {typeof(IFaceInfo<T>)} found");
                }
            }
            else
            {
                _baseInstance = baseInstance;
            }

            _storedFaces = initialDb == null
                ? new ConcurrentDictionary<int, IFaceInfo<T>>()
                : new ConcurrentDictionary<int, IFaceInfo<T>>(initialDb);
        }

        public bool TryGetValue(int id, out IFaceInfo<T> faceInfo)
        {
            return _storedFaces.TryGetValue(id, out faceInfo);
        }

        public IFaceInfo<T> this[int index] => _storedFaces[index];

        public IEnumerable<int> Keys => _storedFaces.Keys;
        public bool ContainsKey(int key) => _storedFaces.ContainsKey(key);

        public string GetName(int id)
        {
            return _storedFaces[id].Name;
        }

        /// <summary>
        /// Will do nothing if a face the same <code>name</code> is already in the database
        /// </summary>
        /// <param name="info"></param>
        /// <param name="id"></param>
        /// <returns><code>true</code> if successful</returns>
        public bool TryAddNewFace(int id, IFaceInfo<T> info)
        {
            bool ret = _storedFaces.TryAdd(id, info);
            if (ret)
            {
                UpdateNextId(id);
            }

            return ret;
        }

        public bool TryAddNewFace(int id, T template, string name = "")
        {
            var faceInfo = _baseInstance.NewInstance();
            faceInfo.AddTemplate(template);
            if (!faceInfo.IsValid(template))
            {
                throw new ArgumentException($"{nameof(template)} invalid!");
            }

            return TryAddNewFace(id, faceInfo);
        }

        private void UpdateNextId(int id)
        {
            if (id >= NextId)
            {
                NextId = id + 1;
            }
        }

        /// <summary>
        /// Get the the <c>id</c> of the face matching <paramref name="template"/> the closest and the <c>confidence</c> of the match
        /// </summary>
        /// <param name="template"></param>
        /// <returns><c>id</c> of the best matching face and <code>confidence</code> value [0, 1]</returns>
        public (int id, float confidence) GetBestMatch(T template)
        {
            (int, float) retValue = (-1, 0);

            var matches = from storedFace in _storedFaces.AsParallel()
                select (storedFace.Key, storedFace.Value.GetSimilarity(template));
            foreach (var match in matches.AsSequential())
            {
                if (match.Item2 > retValue.Item2)
                {
                    retValue = match;
                }
            }

            return retValue;
        }

        public (int id, float confidence) GetBestMatch(IFaceTemplate<T> template)
        {
            (int, float) ret = (-1, 0);
            var matches = from f in _storedFaces.AsParallel()
                let fv = f.Value
                where fv.Age == 0f || (fv.Age / template.Age > 0.75f && fv.Age / template.Age < 1.33f)
                where fv.Gender == template.Gender || fv.Gender == Gender.Unknown
                select (f.Key, fv.GetSimilarity(template));

            foreach (var match in matches.AsSequential())
            {
                if (match.Item2 > ret.Item2)
                {
                    ret = match;
                }
            }

            return ret;
        }

        /// <summary>
        /// Add another faceInfo to existing face -- for example a different angle, with/out glasses, ...
        /// </summary>
        /// <param name="id"></param>
        /// <param name="faceTemplate"></param>
        /// <returns><code>true</code>if succesful</returns>
        /// <exception cref="ArgumentException"> thrown if <code>faceTemplate</code> has incorrect length</exception>
        public bool TryAddFaceTemplateToExistingFace(int id, T faceTemplate)
        {
            if (_storedFaces.TryGetValue(id, out var faceInfo))
            {
                faceInfo.AddTemplate(faceTemplate);
                return true;
            }

            return false;
        }

        public void AddOrUpdate(int id, IFaceTemplate<T> template)
        {
            if (_storedFaces.TryGetValue(id, out var faceInfo))
            {
                faceInfo.AddTemplate(template);
            }
            else
            {
                var newInfo = _baseInstance.NewInstance();
                newInfo.AddTemplate(template);
                if (newInfo.IsValid(template.Template))
                {
                    _storedFaces[id] = newInfo;
                    UpdateNextId(id);
                }
                else
                {
                    throw new ArgumentException($"{nameof(template)} invalid!");
                }
            }
        }

        public void AddOrUpdate(int id, T template)
        {
            if (_storedFaces.TryGetValue(id, out var faceInfo))
            {
                faceInfo.AddTemplate(template);
            }
            else
            {
                var newInfo = _baseInstance.NewInstance();
                newInfo.AddTemplate(template);
                if (newInfo.IsValid(template))
                {
                    _storedFaces[id] = newInfo;
                    UpdateNextId(id);
                }
                else
                {
                    throw new ArgumentException($"{nameof(template)} invalid!");
                }
            }
        }

        public void Add(int id, IFaceInfo<T> faceInfo)
        {
            if (_storedFaces.TryGetValue(id, out var targetFaceInfo))
            {
                targetFaceInfo.Merge(faceInfo);
            }
            else
            {
                _storedFaces[id] = faceInfo;
                UpdateNextId(id);
            }
        }

        /// <summary>
        /// Face with <code>id1</code> gets all the face faceInfo <code>id2</code> has, face with <code>id2</code> is removed
        /// </summary>
        /// <param name="id1">Face to be merged into</param>
        /// <param name="id2">Face to be consumed</param>
        /// <returns><code>true</code> if succesful</returns>
        public bool MergeFaces(int id1, int id2)
        {
            if (_storedFaces.TryGetValue(id1, out var info1) && _storedFaces.TryGetValue(id1, out var info2))
            {
                info1.Merge(info2);
                return _storedFaces.TryRemove(id2, out var _);
            }

            return false;
        }

        public void Serialize(Stream stream)
        {
            var serializer = new FaceDatabaseSerializer(this);
            serializer.Serialize(stream);
        }

        public void Deserialize(Stream stream)
        {
            var deserializer = new FaceDatabaseDeserializer(_baseInstance, this);
            deserializer.Deserialize(stream);
        }

        public object Clone()
        {
            var ret = new FaceDatabase<T>(_baseInstance, _storedFaces)
            {
                SerializePath = this.SerializePath,
                NextId = this.NextId
            };

            return ret;
        }
    }
}