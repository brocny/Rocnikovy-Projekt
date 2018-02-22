using System;
using System.IO;
using System.Linq;

namespace Face
{
    public partial class FaceDatabase<T>
    {
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
                    if (!int.TryParse(Path.GetFileNameWithoutExtension(filePath), out int id)) continue;
                    using (var fileStream = File.OpenRead(filePath))
                    {
                        var fInfo = _faceInfoBaseInstance.Deserialize(fileStream);
                        _db.Add(id, fInfo);
                    }
                }
            }
        }
    }
}

