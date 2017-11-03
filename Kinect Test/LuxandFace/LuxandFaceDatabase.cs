using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Luxand;

namespace LuxandFace
{
    public class LuxandFaceDatabase
    {
        private Dictionary<string, FaceInfo> _storedFaces;

        public LuxandFaceDatabase()
        {
            _storedFaces = new Dictionary<string, FaceInfo>();
        }

        public bool TryAddNewFace(string name, FaceInfo info)
        {
            if (_storedFaces.ContainsKey(name))
            {
                return false;
            }
            _storedFaces.Add(name, info);
            return true;
        }

        public (string name, float confidence) GetMostLikelyMatch(byte[] template)
        {
            string outName = string.Empty;
            float outConfidence = 0;

            foreach (var pair in _storedFaces)
            {
                float similarity;
                if ((similarity = pair.Value.GetSimilarity(template)) > outConfidence)
                {
                    outConfidence = similarity;
                    outName = pair.Key;
                }
            }

            return (outName, outConfidence);
        }

        public bool TryAddFaceTemplateToExistingFace(string name, byte[] faceTemplate)
        {
            FaceInfo.ThrowIfTemplateLengthInvalid(faceTemplate);
            if (_storedFaces.TryGetValue(name, out var faceInfo))
            {
                faceInfo.AddTemplate(faceTemplate);
                return true;
            }
            return false;
        }

        public bool Merge(string name1, string name2)
        {
            if (_storedFaces.TryGetValue(name1, out var info1) && _storedFaces.TryGetValue(name2, out var info2))
            {
                info2.Merge(info1);
                _storedFaces.Remove(name1);
                return true;
            }

            return false;
        }

    }


    public class FaceInfo
    {
        public List<byte[]> FaceTemplates { get; }

        public FaceInfo(byte[] faceTemplate)
        {
            FaceTemplates = new List<byte[]>{faceTemplate};
        }

        public void Merge(FaceInfo info)
        {
            FaceTemplates.AddRange(info.FaceTemplates);
        }

        public void AddTemplate(byte[] faceTemplate)
        {
            FaceTemplates.Add(faceTemplate);
        }

        internal static void ThrowIfTemplateLengthInvalid(byte[] template)
        {
            if (template.Length != FSDK.TemplateSize)
            {
                throw new ArgumentException($"faceTemplate of length {FSDK.TemplateSize} expected, got length {template.Length}");
            }
        }

        public float GetSimilarity(byte[] faceTemplate)
        {
            float maxSimilarity = 0;
            float avgSimilarity = 0;
            int matchedTemplates = 0;
            foreach (var i in FaceTemplates)
            {
                float sim = 0;
                var ft = i;
                if(FSDK.FSDKE_OK == FSDK.MatchFaces(ref faceTemplate, ref ft, ref sim))
                {
                    matchedTemplates++;
                    maxSimilarity = Math.Max(sim, maxSimilarity);
                    avgSimilarity += sim;
                }
            }

            return (maxSimilarity * 3 + avgSimilarity) / 2;
        }
       
    }
}