using System.IO;
using System.Xml;

namespace Core.Face
{
    public partial class DictionaryFaceDatabase<TTemplate>
    {
        public class FaceDatabaseDeserializer
        {
            private readonly DictionaryFaceDatabase<TTemplate> _db;
            private readonly IFaceInfo<TTemplate> _faceInfoBaseInstance;

            public FaceDatabaseDeserializer(IFaceInfo<TTemplate> faceInfoBaseInstance, DictionaryFaceDatabase<TTemplate> db)
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