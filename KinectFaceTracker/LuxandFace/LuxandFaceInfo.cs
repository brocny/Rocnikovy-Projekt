using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Face;
using Luxand;

namespace LuxandFaceLib
{
    [Serializable]
    public class LuxandFaceInfo : IFaceInfo<byte[]>
    {
        [XmlElement(IsNullable = true, ElementName = "GenderConf")]
        public float? GenderConfidence { get; set; }
        [XmlIgnore]
        public IReadOnlyCollection<byte[]> Templates => _faceTemplates;
        [XmlAttribute("Name")]
        public string Name { get; set; }
        [XmlElement(IsNullable = true, ElementName = "Age")]
        public float? Age { get; set; }
        [XmlElement("Gender")]
        public Gender Gender { get; set; }

        public LuxandFaceInfo()
        {

        }

        public LuxandFaceInfo(byte[] faceTemplate)
        {
            if(faceTemplate != null) _faceTemplates.Add(faceTemplate);
        }

        public void Merge(IFaceInfo<byte[]> info)
        {
            _faceTemplates.AddRange(info.Templates);
        }

        public float GetSimilarity(IFaceInfo<byte[]> faceInfo)
        {
            return _faceTemplates.Max(x => faceInfo.GetSimilarity(x));
        }

        public void AddTemplate(byte[] faceTemplate)
        {
            _faceTemplates.Add(faceTemplate);
        }

        public void AddTemplates(IEnumerable<byte[]> faceTemplates)
        {
            _faceTemplates.AddRange(faceTemplates);
        }

        public bool IsValid(byte[] template)
        {
            return template != null && template.Length == FSDK.TemplateSize;
        }

        public float GetSimilarity(byte[] faceTemplate)
        {
            if (faceTemplate == null || _faceTemplates.Count == 0) return 0;
            float[] similarities = new float[_faceTemplates.Count];
            Parallel.For(0, _faceTemplates.Count, i =>
            {
                var ithTemplate = _faceTemplates[i];
                if (FSDK.FSDKE_OK != FSDK.MatchFaces(ref faceTemplate, ref ithTemplate, ref similarities[i]))
                {
                    similarities[i] = 0;
                }
            });

            return similarities.Max();
        }

        public IFaceInfo<byte[]> NewInstance()
        {
            return new LuxandFaceInfo();
        }

        public void Serialize(Stream stream)
        {
            XmlSerializer x = new XmlSerializer(typeof(LuxandFaceInfo));
            x.Serialize(stream, this);
        }

        public IFaceInfo<byte[]> Deserialize(Stream stream)
        {
            var x = new XmlSerializer(typeof(LuxandFaceInfo));
            var ret = (LuxandFaceInfo) x.Deserialize(stream);
            return ret;
        }

        [XmlIgnore]
        private List<byte[]> _faceTemplates = new List<byte[]>();

        [XmlArrayItem("Template")]
        [XmlArray("Templates")]
        public List<string> XmlFaceTemplates
        {
            get => _faceTemplates.Select(x => Encoding.Default.GetString(x)).ToList();
            set => _faceTemplates = value.Select(x => Encoding.Default.GetBytes(x)).ToList();
        }
    }
}