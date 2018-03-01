using System.IO;

namespace Face
{
    public partial class FaceDatabase<T>
    {
        public class FaceDatabaseSerializer
        {
            public FaceDatabaseSerializer(FaceDatabase<T> faceDatabase)
            {
                _faceDatabase = faceDatabase;
            }

            public FaceDatabaseSerializer()
            {
            }

            public void Serialize(string outputDir = "faces")
            {
                foreach (var face in _faceDatabase._storedFaces)
                {
                    using (var fileStream = File.Open($"{outputDir}/{face.Key}.xml", FileMode.Create, FileAccess.Write))
                    {
                        face.Value.Serialize(fileStream);
                    }
                }
            }

            private readonly FaceDatabase<T> _faceDatabase;
        }
    }
}

