﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Face;
using Luxand;

namespace LuxandFaceLib
{
    public class LuxandFaceInfo : IFaceInfo<byte[]>
    {
        public float? GenderConfidence { get; set; }
        public IReadOnlyCollection<byte[]> Templates => _faceTemplates;
        public string Name { get; set; }
        public float? Age { get; set; }
        public Gender Gender { get; set; }

        public LuxandFaceInfo()
        {
            _faceTemplates = new List<byte[]>();
        }

        public LuxandFaceInfo(byte[] faceTemplate)
        {
            _faceTemplates = new List<byte[]>{faceTemplate};
        }

        public void Merge(IFaceInfo<byte[]> info)
        {
            _faceTemplates.AddRange(info.Templates);
        }

        public float GetSimilarity(IFaceInfo<byte[]> faceInfo)
        {
            throw new System.NotImplementedException();
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
                if (FSDK.FSDKE_OK == FSDK.MatchFaces(ref faceTemplate, ref ithTemplate, ref similarities[i]))
                {
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
            var writer = new BinaryWriter(stream);
            writer.Write(Name ?? "");
            foreach (var template in _faceTemplates)
            {
                writer.Write(template, 0, template.Length);
            }
        }

        public IFaceInfo<byte[]> Deserialize(Stream stream)
        {
            var ret = NewInstance();
            using (var reader = new BinaryReader(stream))
            {
                ret.Name = reader.ReadString();
                while (reader.PeekChar() >= 0)
                {
                    var buffer = new byte[FSDK.TemplateSize];
                    stream.Read(buffer, 0, FSDK.TemplateSize);
                    ret.AddTemplate(buffer);
                }
            }
                
            return ret;
        }

        private List<byte[]> _faceTemplates;

        private const float WeightAvgMatch = 1;
        private const float WeightMaxMatch = 5;

       
    }
}