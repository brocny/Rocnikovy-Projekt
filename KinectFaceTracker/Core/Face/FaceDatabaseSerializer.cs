using System.IO;
using System.Xml;

namespace Core.Face
{
    public partial class DictionaryFaceDatabase<TTemplate>
    {
        public class FaceDatabaseSerializer
        {
            private readonly DictionaryFaceDatabase<TTemplate> _faceDatabase;

            public FaceDatabaseSerializer(DictionaryFaceDatabase<TTemplate> faceDatabase)
            {
                _faceDatabase = faceDatabase;
            }

            public void Serialize(Stream stream)
            {
                using (var sw = new StreamWriter(stream))
                {
                    var xw = XmlWriter.Create(sw, new XmlWriterSettings {Indent = true});
                    xw.WriteStartDocument();
                    xw.WriteStartElement("Faces");
                    foreach (var face in _faceDatabase._storedFaces)
                    {
                        xw.WriteStartElement("FaceInfo");
                        xw.WriteElementString("Id", face.Key.ToString());
                        xw.Flush();
                        sw.WriteLine();
                        sw.Flush();
                        face.Value.Serialize(sw);
                        xw.WriteEndElement();
                    }

                    xw.WriteEndElement();
                    xw.WriteEndDocument();
                    xw.Flush();
                    sw.Flush();
                }
            }
        }
    }
}