using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Face;
using Luxand;

namespace LuxandFaceLib
{
    [Serializable]
    public class LuxandFaceInfo : IFaceInfo<byte[]>
    {
        [XmlElement(ElementName = "GenderConf")]
        public float GenderConfidence
        {
            get
            {
                if (_confidenceMale > GenderMinConfidence) return _confidenceMale;
                if (_confidenceMale < 1f - GenderMinConfidence) return 1 - _confidenceMale;
                return 0;
            }
        }
        [XmlIgnore]
        public IReadOnlyCollection<byte[]> Templates => _faceTemplates;
        [XmlElement(ElementName = "Name")]
        public string Name { get; set; }
        [XmlElement(ElementName = "Age")]
        public float Age { get; set; }

        [XmlElement("Gender")]
        public Gender Gender
        {
            get
            {
                const float minTotalConf = 0.95f, confPerTemplate = 0.05f;

                if (_confidenceMale + confPerTemplate * _faceTemplates.Count < minTotalConf 
                    && 1f - _confidenceMale + confPerTemplate * _faceTemplates.Count < minTotalConf)
                    return Gender.Unknown;
                if (_confidenceMale > GenderMinConfidence) return Gender.Male;
                if (_confidenceMale < 1f - GenderMinConfidence) return Gender.Female;
                return Gender.Unknown;
            }
        }

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

        public float GetSimilarity(IFaceTemplate<byte[]> faceTemplate)
        {
            const float wrongGenderPenalty = 0.75f,
                maxAgeRatioWithoutPenalty = 1.25f,
                minAgeRatioWithoutPenalty = 1f / maxAgeRatioWithoutPenalty;

            float baseSim = GetSimilarity(faceTemplate.Template);
            
            float ageRatio = Age / faceTemplate.Age;
            if (ageRatio > maxAgeRatioWithoutPenalty) baseSim /= ageRatio;
            if (ageRatio < minAgeRatioWithoutPenalty) baseSim *= ageRatio;


            if (faceTemplate.Gender != Gender.Unknown && faceTemplate.Gender != Gender)
            {
                baseSim *= wrongGenderPenalty;
            }

            return baseSim;
        }

        public void AddTemplate(byte[] faceTemplate)
        {
            _faceTemplates.Add(faceTemplate);
        }

        public void AddTemplate(IFaceTemplate<byte[]> faceTemplate)
        {
            int ftCount = _faceTemplates.Count;

            // Age is the average of observed ages
            Age = (Age * ftCount + faceTemplate.Age) / (ftCount + 1);

            // Gender confidence is the average of observations
            if (faceTemplate.Gender != Gender.Unknown)
            {
                _confidenceMale = (_confidenceMale * ftCount
                                  + (faceTemplate.Gender == Gender.Male ? faceTemplate.GenderConfidence: 1 - faceTemplate.GenderConfidence)) 
                                  / (ftCount + 1);
            }
            _faceTemplates.Add(faceTemplate.Template);
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
            var xw = XmlWriter.Create(stream, new XmlWriterSettings {OmitXmlDeclaration = true, CheckCharacters = false, Indent = true});
            XmlSerializer x = new XmlSerializer(typeof(LuxandFaceInfo));
            x.Serialize(xw, this);
        }

        public void Serialize(TextWriter writer)
        {
            var xw = XmlWriter.Create(writer,
                new XmlWriterSettings {OmitXmlDeclaration = true, CheckCharacters = false, Indent = true});
            XmlSerializer x = new XmlSerializer(typeof(LuxandFaceInfo));
            x.Serialize(xw,this);
        }

        public IFaceInfo<byte[]> Deserialize(Stream stream)
        {
            var x = new XmlSerializer(typeof(LuxandFaceInfo));
            var ret = (LuxandFaceInfo) x.Deserialize(stream);
            return ret;
        }
        

        public IFaceInfo<byte[]> Deserialize(TextReader reader)
        {
            var xr = XmlReader.Create(reader, new XmlReaderSettings {CheckCharacters = false});
            var x = new XmlSerializer(typeof(LuxandFaceInfo));
            var ret = (LuxandFaceInfo) x.Deserialize(xr);
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

        private float _confidenceMale;

        private const float GenderMinConfidence = 0.75f;
    }
}