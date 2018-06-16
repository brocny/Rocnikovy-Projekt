using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;

namespace Core.Face
{
    /// <summary>
    /// A database for storing information about faces
    /// </summary>
    /// <remarks>
    /// Thread-safe 
    /// </remarks>
    /// <typeparam name="TTemplate">Type of face templates</typeparam>
    [Serializable]
    public partial class DictionaryFaceDatabase<TTemplate> : IFaceDatabase<TTemplate>, IEnumerable<KeyValuePair<int, IFaceInfo<TTemplate>>>
    {
        private ConcurrentDictionary<int, IFaceInfo<TTemplate>> _storedFaces;
        private readonly IFaceInfo<TTemplate> _baseInstance;

        public IEnumerable<KeyValuePair<int, IFaceInfo<TTemplate>>> Pairs => _storedFaces.Select(x => x);

        public int NextId { get; private set; }
        public string SerializePath { get; set; }

        public DictionaryFaceDatabase(IFaceInfo<TTemplate> baseInstance = null, IEnumerable<KeyValuePair<int, IFaceInfo<TTemplate>>> initialDb = null)
        {
            if (baseInstance == null)
            {
                var firstType = (from t in Assembly.GetExecutingAssembly().GetExportedTypes()
                        where !t.IsAbstract && !t.IsInterface && typeof(IFaceInfo<TTemplate>).IsAssignableFrom(t) && t.GetConstructor(Type.EmptyTypes) != null
                        select t)
                    .FirstOrDefault();

                if (firstType != null)
                {
                    _baseInstance = (IFaceInfo<TTemplate>)Activator.CreateInstance(firstType);
                }
                else
                {
                    throw new ApplicationException($"No suitable class implementing {typeof(IFaceInfo<TTemplate>)} found");
                }
            }
            else
            {
                _baseInstance = baseInstance;
            }

            _storedFaces = initialDb == null
                ? new ConcurrentDictionary<int, IFaceInfo<TTemplate>>()
                : new ConcurrentDictionary<int, IFaceInfo<TTemplate>>(initialDb);
        }

        public bool TryGetValue(int id, out IFaceInfo<TTemplate> faceInfo)
        {
            return _storedFaces.TryGetValue(id, out faceInfo);
        }

        public IFaceInfo<TTemplate> this[int faceId] => _storedFaces[faceId];
        [XmlIgnore]
        public IEnumerable<int> Keys => _storedFaces.Keys;
        [XmlIgnore]
        public IEnumerable<IFaceInfo<TTemplate>> Values => _storedFaces.Values;
        public bool ContainsKey(int key) => _storedFaces.ContainsKey(key);

        public string GetName(int id)
        {
            return _storedFaces.TryGetValue(id, out var faceInfo) ? faceInfo.Name : null;
        }

        /// <summary>
        /// Will do nothing if a face the same <paramref name="id"/> is already in the database
        /// </summary>
        /// <param name="info"></param>
        /// <param name="id"></param>
        /// <returns><c>true</c> if successful</returns>
        public bool TryAddNewFace(int id, IFaceInfo<TTemplate> info)
        {
            bool ret = _storedFaces.TryAdd(id, info);
            if (ret)
            {
                UpdateNextId(id);
            }

            return ret;
        }

        public bool TryAddNewFace(int id, TTemplate template, string name = "")
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
        /// <returns><c>id</c> of the best matching face and <c>confidence</c> value [0, 1]</returns>
        public Match<TTemplate> GetBestMatch(TTemplate template)
        {
            var matches = from f in _storedFaces.AsParallel()
                let faceInfo = f.Value
                let match = faceInfo.GetSimilarity(template)
                select new Match<TTemplate>(f.Key, match.similarity, match.snapshot, faceInfo);

            return matches.Max();
        }

        public Match<TTemplate> GetBestMatch(IFaceTemplate<TTemplate> template)
        {
            var matches = from f in _storedFaces.AsParallel()
                let faceInfo = f.Value
                let ageRatio = template.Age == 0 || faceInfo.Age == 0 ? 1 : faceInfo.Age / template.Age
                where ageRatio > 0.66f && ageRatio < 1.5f
                where faceInfo.Gender == template.Gender || faceInfo.Gender == Gender.Unknown
                let match = faceInfo.GetSimilarity(template)
                select new Match<TTemplate>(f.Key, match.similarity, match.snapshot, faceInfo, template.TrackingId);

            return matches.Max();
        }

        /// <summary>
        /// Add another template to an existing face -- for example a different angle, with/out glasses, ...
        /// </summary>
        /// <param name="id"></param>
        /// <param name="faceTemplate"></param>
        /// <returns><c>true</c>if succesful</returns>
        /// <exception cref="ArgumentException"> thrown if <paramref name="faceTemplate"/> has incorrect length</exception>
        public bool TryAddFaceTemplateToExistingFace(int id, TTemplate faceTemplate)
        {
            if (_storedFaces.TryGetValue(id, out var faceInfo))
            {
                faceInfo.AddTemplate(faceTemplate);
                return true;
            }

            return false;
        }

        public void AddOrUpdate(int id, IFaceTemplate<TTemplate> template)
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

        public void AddOrUpdate(int id, TTemplate template)
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

        public void Clear()
        {
            _storedFaces.Clear();
        }

        public void Add(int id, IFaceInfo<TTemplate> faceInfo)
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
            if (!_storedFaces.TryGetValue(id1, out var info1) || !_storedFaces.TryGetValue(id1, out var info2))
                return false;

            info1.Merge(info2);
            return _storedFaces.TryRemove(id2, out var _);
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

        public IFaceDatabase<TTemplate> Backup()
        {
            return new DictionaryFaceDatabase<TTemplate>(_baseInstance, _storedFaces)
            {
                SerializePath = SerializePath,
                NextId = NextId
            };
        }

        public void Restore(IFaceDatabase<TTemplate> backup)
        {
            _storedFaces = new ConcurrentDictionary<int, IFaceInfo<TTemplate>>(backup.Pairs);
            NextId = backup.NextId;
        }

        public IEnumerator<KeyValuePair<int, IFaceInfo<TTemplate>>> GetEnumerator()
        {
            return _storedFaces.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}