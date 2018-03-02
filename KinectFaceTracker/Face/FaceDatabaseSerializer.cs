using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;

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
                        face.Value.Serialize(stream);
                        xw.WriteEndElement();
                    }
                    xw.WriteEndElement();
                    xw.WriteEndDocument();
                    xw.Flush();
                    sw.Flush();
                }
            }

            private readonly FaceDatabase<T> _faceDatabase;
        }
    }
}

