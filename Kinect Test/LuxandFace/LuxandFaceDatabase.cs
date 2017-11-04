using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Luxand;

namespace LuxandFaceLib
{
    public class LuxandFaceDatabase
    {
        private Dictionary<string, FaceInfo> _storedFaces;

        public LuxandFaceDatabase()
        {
            _storedFaces = new Dictionary<string, FaceInfo>();
        }

        /// <summary>
        /// Will do nothing if a face the same <code>name</code> is already in the database
        /// </summary>
        /// <param name="name"></param>
        /// <param name="info"></param>
        /// <returns><code>true</code> if successful</returns>
        public bool TryAddNewFace(string name, FaceInfo info)
        {
            if (_storedFaces.ContainsKey(name))
            {
                return false;
            }
            _storedFaces.Add(name, info);
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="template"></param>
        /// <returns><code>name</code> of the best matching face and <code>confidence</code> value [0, 1]</returns>
        public (string name, float confidence) GetBestMatch(byte[] template)
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

        /// <summary>
        /// Add another template to existing face -- for example a different angle, with/out glasses, ...
        /// </summary>
        /// <param name="name"></param>
        /// <param name="faceTemplate"></param>
        /// <returns><code>true</code>if succesful</returns>
        /// <exception cref="ArgumentException"> thrown if <code>faceTemplate</code> has incorrect length</exception>
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

        /// <summary>
        /// Merge face with the name <code>name2</code> into the face with the name <code>name1</code>
        /// </summary>
        /// <param name="name1"></param>
        /// <param name="name2"></param>
        /// <returns>True if succesful</returns>
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
        public ICollection<byte[]> FaceTemplates => _faceTemplates;
        private List<byte[]> _faceTemplates;

        private const float WeightAvgMatch = 1;
        private const float WeightMaxMatch = 3;

        public FaceInfo(byte[] faceTemplate)
        {
            _faceTemplates = new List<byte[]>{faceTemplate};
        }

        public void Merge(FaceInfo info)
        {
            _faceTemplates.AddRange(info.FaceTemplates);
        }

        public void AddTemplate(byte[] faceTemplate)
        {
            _faceTemplates.Add(faceTemplate);
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
            foreach (var i in _faceTemplates)
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

            return (maxSimilarity * WeightMaxMatch + avgSimilarity * WeightAvgMatch) / (WeightMaxMatch + WeightAvgMatch);
        }
       
    }
}