using System;
using System.IO;
using System.Linq;
using System.Xml;

namespace Face
{
    public partial class DictionaryFaceDatabase<T>
    {
        public class FaceDatabaseDeserializer
        {
            private readonly IFaceInfo<T> _faceInfoBaseInstance;
            private readonly DictionaryFaceDatabase<T> _db;

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

                        var id = xr.ReadElementContentAsInt();
                        xr.ReadToFollowing("LuxandFaceInfo");
                        var sr = new StringReader(xr.ReadOuterXml());
                        _db._storedFaces[id] = _faceInfoBaseInstance.Deserialize(sr);
                        _db.UpdateNextId(id);
                    }
                }
            }

        }
    }
}

