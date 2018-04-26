using System.IO;
using System.Xml;

namespace Face
{
    public partial class DictionaryFaceDatabase<T>
    {
        public class FaceDatabaseDeserializer
        {
            private readonly DictionaryFaceDatabase<T> _db;
            private readonly IFaceInfo<T> _faceInfoBaseInstance;

            public FaceDatabaseDeserializer(IFaceInfo<T> faceInfoBaseInstance, DictionaryFaceDatabase<T> db)
            {
                _faceInfoBaseInstance = faceInfoBaseInstance;
                _db = db;
            }

            public void Deserialize(Stream stream)
            {
                using (var xr = XmlReader.Create(stream, new XmlReaderSettings {CheckCharacters = false}))
                {
                    while (xr.ReadToFollowing("Id"))
                    {
                        int id = xr.ReadElementContentAsInt();
                        xr.ReadToFollowing("IFaceInfo");
                        var sr = new StringReader(xr.ReadOuterXml());
                        _db._storedFaces[id] = _faceInfoBaseInstance.Deserialize(sr);
                        _db.UpdateNextId(id);
                    }
                }
            }
        }
    }
}