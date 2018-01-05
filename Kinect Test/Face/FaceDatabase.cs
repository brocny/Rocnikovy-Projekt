using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Face
{
    /// <summary>
    /// A database for storing information about faces
    /// </summary>
    /// <remarks>
    /// Thread-safe 
    /// </remarks>
    /// <typeparam name="T">Type of face templates</typeparam>
    public class FaceDatabase<T>
    {
        private ConcurrentDictionary<int, IFaceInfo<T>> _storedFaces;
        private IFaceInfo<T> _baseInstance;

        public int NextId { get; private set; }

        public FaceDatabase(IFaceInfo<T> baseInstance, IDictionary<int, IFaceInfo<T>> initialDb = null)
        {
            _baseInstance = baseInstance;
            _storedFaces = initialDb == null
                ? new ConcurrentDictionary<int, IFaceInfo<T>>()
                : new ConcurrentDictionary<int, IFaceInfo<T>>(initialDb);
        }

        public FaceDatabase(IDictionary<int, IFaceInfo<T>> initialDb = null)
        {
            _baseInstance = Activator.CreateInstance<IFaceInfo<T>>();
            _storedFaces = initialDb == null
                ? new ConcurrentDictionary<int, IFaceInfo<T>>()
                : new ConcurrentDictionary<int, IFaceInfo<T>>(initialDb);
        }

        public IFaceInfo<T> GetFaceInfo(int id) => _storedFaces[id];

        public IEnumerable<int> GetAllIDs()
        {
            return _storedFaces.Keys;
        }

        public string GetName(int id)
        {
            return _storedFaces[id].Name;
        }
        /// <summary>
        /// Will do nothing if a face the same <code>name</code> is already in the database
        /// </summary>
        /// <param name="name"></param>
        /// <param name="info"></param>
        /// <returns><code>true</code> if successful</returns>
        public bool TryAddNewFace(int id, IFaceInfo<T> info, string name = "")
        {
            UpdateNextId(id);
            return _storedFaces.TryAdd(id, info);
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
        /// 
        /// </summary>
        /// <param name="template"></param>
        /// <returns><code>name</code> of the best matching face and <code>confidence</code> value [0, 1]</returns>
        public (int id, float confidence) GetBestMatch(T template)
        {
            (int, float) retValue = (0, 0);

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

        public void AddOrUpdate(int id, T faceTemlate)
        {
            if (_storedFaces.TryGetValue(id, out var faceInfo))
            {
                faceInfo.AddTemplate(faceTemlate);
            }
            else
            {
                var newInfo = _baseInstance.NewInstance();
                newInfo.AddTemplate(faceTemlate);
                if (newInfo.IsValid(faceTemlate))
                {
                    _storedFaces[id] = newInfo;
                    UpdateNextId(id);
                }
                else
                {
                    throw new ArgumentException($"{nameof(faceTemlate)} invalid!");
                }
            }
        }

        public void Add(int id, IFaceInfo<T> template)
        {
            if (_storedFaces.TryGetValue(id, out var faceInfo))
            {
                faceInfo.Merge(template);
            }
            else
            {
                _storedFaces[id] = template;
                UpdateNextId(id);
            }
        }

        /// <summary>
        /// Face with <code>id1</code> gets all the face template <code>id2</code> has, face with <code>id2</code> is removed
        /// </summary>
        /// <param name="id1">Face to be merged into</param>
        /// <param name="id2">Face to be consumed</param>
        /// <returns><code>true</code> if succesful</returns>
        public bool Merge(int id1, int id2)
        {
            if (_storedFaces.TryGetValue(id1, out var info1) && _storedFaces.TryGetValue(id1, out var info2))
            {
                info1.Merge(info2);
                return _storedFaces.TryRemove(id2, out var _);
            }

            return false;
        }

        public void Serialize(string dir)
        {
            var serializer = new FaceDatabaseSerializer(this);
            serializer.Serialize(dir);
        }

        public void Deserialize(string dir)
        {
            var deserializer = new FaceDatabaseDeserializer(_baseInstance, this);
            deserializer.Deserialize(dir);
        }

        public class FaceDatabaseSerializer
        {
            public FaceDatabaseSerializer(FaceDatabase<T> faceDatabase)
            {
                _faceDatabase = faceDatabase;
            }

            public void Serialize(string outputDir = "faces")
            {
                foreach (var face in _faceDatabase._storedFaces)
                {
                    using (var file = File.Open($"{outputDir}/{face.Key}.bin", FileMode.Create, FileAccess.Write))
                        face.Value.Serialize(file);
                }
            }

            private readonly FaceDatabase<T> _faceDatabase;
        }

        public class FaceDatabaseDeserializer
        {
            private IFaceInfo<T> _faceInfoBaseInstance;
            private FaceDatabase<T> _db;

            public FaceDatabaseDeserializer(IFaceInfo<T> faceInfoBaseInstance, FaceDatabase<T> db)
            {
                _faceInfoBaseInstance = faceInfoBaseInstance;
                _db = db;

            }

            public void Deserialize(string dir = "faces")
            {
                if (!Directory.Exists(dir))
                {
                    throw new ArgumentException($"{dir} is not a valid directory path!");
                }

                var filePaths = Directory.EnumerateFiles(dir).Where(f => Path.GetExtension(f) == ".bin");

                foreach (var filePath in filePaths)
                {
                    var id = int.Parse(Path.GetFileNameWithoutExtension(filePath));
                    using (var file = File.OpenRead(filePath))
                    using (var stream = new FileStream(file.SafeFileHandle, FileAccess.Read))
                    {
                        var fInfo = _faceInfoBaseInstance.Deserialize(stream);
                        _db.Add(id, fInfo);
                    }
                    
                }
            }
        }
    }
}

