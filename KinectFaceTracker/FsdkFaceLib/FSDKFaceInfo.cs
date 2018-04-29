using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Core.Face;
using Core;
using Luxand;
using MoreLinq;


namespace FsdkFaceLib
{
    [Serializable]
    [XmlRoot("IFaceInfo")]
    public class FSDKFaceInfo : IFaceInfo<byte[]>
    {
        [XmlIgnore]
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
        public IEnumerable<byte[]> Templates => Snapshots.Select(x => x.Template);

        [XmlIgnore]
        public IEnumerable<Core.ImageBuffer> Images => Snapshots.Select(x => x.FaceImageBuffer);

        [XmlElement(ElementName = "Name")]
        public string Name { get; set; }

        [XmlElement(ElementName = "Age")]
        public float Age { get; set; }

        public float ConfidenceMale
        {
            get => _confidenceMale;
            set => _confidenceMale = value;
        }


        [XmlIgnore]
        public Gender Gender
        {
            get
            {
                const float minTotalConf = 0.95f, confPerTemplate = 0.05f;

                if (_confidenceMale + confPerTemplate * Snapshots.Count < minTotalConf 
                    && 1f - _confidenceMale + confPerTemplate * Snapshots.Count < minTotalConf)
                    return Gender.Unknown;
                if (_confidenceMale > GenderMinConfidence) return Gender.Male;
                if (_confidenceMale < 1f - GenderMinConfidence) return Gender.Female;
                return Gender.Unknown;
            }
        }

        public FSDKFaceInfo()
        {

        }

        public FSDKFaceInfo(byte[] faceTemplate)
        {
            if(faceTemplate != null) Snapshots.Add(new FaceSnapshotByteArray(faceTemplate, null));
        }

        public void Merge(IFaceInfo<byte[]> info)
        {
            Snapshots.AddRange(info.Snapshots);
        }

        public (float similarity, FaceSnapshot<byte[]> snapshot) GetSimilarity(IFaceInfo<byte[]> faceInfo)
        {
            return Snapshots.Max(x => faceInfo.GetSimilarity(x.Template));
        }

        public (float similarity, FaceSnapshot<byte[]> snapshot) GetSimilarity(IFaceTemplate<byte[]> faceTemplate)
        {
            const float wrongGenderPenalty = 0.75f,
                maxAgeRatioWithoutPenalty = 1.25f,
                minAgeRatioWithoutPenalty = 1f / maxAgeRatioWithoutPenalty;

            var baseMatch = GetSimilarity(faceTemplate.Template);
            
            float ageRatio = Age / faceTemplate.Age;
            if (ageRatio > maxAgeRatioWithoutPenalty) baseMatch.similarity /= ageRatio;
            if (ageRatio < minAgeRatioWithoutPenalty) baseMatch.similarity *= ageRatio;


            if (faceTemplate.Gender != Gender.Unknown && faceTemplate.Gender != Gender)
            {
                baseMatch.similarity *= wrongGenderPenalty;
            }

            return baseMatch;
        }

        public void AddTemplate(byte[] faceTemplate, Core.ImageBuffer imageBuffer = null)
        {
            Snapshots.Add(new FaceSnapshotByteArray(faceTemplate, imageBuffer));
        }

        public void AddTemplate(IFaceTemplate<byte[]> faceTemplate)
        {
            int ftCount = Snapshots.Count;

            // Age is the average of observed ages
            Age = (Age * ftCount + faceTemplate.Age) / (ftCount + 1);

            // Gender confidence is the average of observations
            if (faceTemplate.Gender != Gender.Unknown)
            {
                _confidenceMale = (_confidenceMale * ftCount
                                  + (faceTemplate.Gender == Gender.Male ? faceTemplate.GenderConfidence: 1 - faceTemplate.GenderConfidence)) 
                                  / (ftCount + 1);
            }
            Snapshots.Add(new FaceSnapshotByteArray(faceTemplate.Template, faceTemplate.FaceImage));
        }

        public void AddTemplates(IEnumerable<byte[]> faceTemplates)
        {
            Snapshots.AddRange(faceTemplates.Select(x => new FaceSnapshotByteArray(x, null)));
        }

        public bool IsValid(byte[] template)
        {
            return template != null && template.Length == FSDK.TemplateSize;
        }

        public (float similarity, FaceSnapshot<byte[]> snapshot) GetSimilarity(byte[] faceTemplate)
        {
            if (faceTemplate == null || Snapshots.Count == 0) return default;
            var similarities = new (float similarity, FaceSnapshot<byte[]> faceSnapshot)[Snapshots.Count];
            Parallel.For(0, Snapshots.Count, i =>
            {
                var ithTemplate = Snapshots[i].Template;
                if (FSDK.FSDKE_OK != FSDK.MatchFaces(ref faceTemplate, ref ithTemplate, ref similarities[i].similarity))
                {
                    similarities[i] = default;
                }

                similarities[i].faceSnapshot = Snapshots[i];
            });

            return similarities.MaxBy(x => x.similarity);
        }

        public IFaceInfo<byte[]> NewInstance()
        {
            return new FSDKFaceInfo();
        }

        public void Serialize(Stream stream)
        {
            var xw = XmlWriter.Create(stream, new XmlWriterSettings {OmitXmlDeclaration = true, Indent = true});
            var xs = new XmlSerializer(typeof(FSDKFaceInfo), new[] { typeof(FaceSnapshotByteArray)});
            xs.Serialize(xw, this);
        }

        public void Serialize(TextWriter writer)
        {
            var xw = XmlWriter.Create(writer,
                new XmlWriterSettings {OmitXmlDeclaration = true, Indent = true});
            var xs = new XmlSerializer(typeof(FSDKFaceInfo), new [] {typeof(FaceSnapshotByteArray)});
            xs.Serialize(xw, this);
        }

        public IFaceInfo<byte[]> Deserialize(Stream stream)
        {
            var xr = XmlReader.Create(stream, new XmlReaderSettings {IgnoreWhitespace = true});
            var xs = new XmlSerializer(typeof(FSDKFaceInfo), new [] {typeof(FaceSnapshotByteArray)});
            var ret = (FSDKFaceInfo) xs.Deserialize(stream);
            return ret;
        }
        

        public IFaceInfo<byte[]> Deserialize(TextReader reader)
        {
            var xr = XmlReader.Create(reader, new XmlReaderSettings {IgnoreWhitespace = true});
            var xs = new XmlSerializer(typeof(FSDKFaceInfo), new [] {typeof(FaceSnapshotByteArray)});
            var ret = (FSDKFaceInfo) xs.Deserialize(xr);
            return ret;
        }
        
        [XmlArray("Snapshots")]
        public List<FaceSnapshot<byte[]>> Snapshots { get; set; } = new List<FaceSnapshot<byte[]>>();

        [XmlIgnore]
        IEnumerable<FaceSnapshot<byte[]>> IFaceInfo<byte[]>.Snapshots => Snapshots;


        private float _confidenceMale;

        private const float GenderMinConfidence = 0.75f;
    }

    
    [Serializable]
    public class FaceSnapshotByteArray : FaceSnapshot<byte[]>
    {
        public FaceSnapshotByteArray() { }

        public FaceSnapshotByteArray(byte[] template, Core.ImageBuffer imageBuffer) : base(template, imageBuffer) { }

        [Browsable(false)]
        [XmlElement("Template")]
        public override string XmlTemplate
        {
            get => Convert.ToBase64String(Template);
            set => Template = Convert.FromBase64String(value);
        }
    }
}