using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
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

        public bool TryAddNewFace(string name, byte[] template)
        {
            FaceInfo.ThrowIfTemplateLengthInvalid(template);
            return TryAddNewFace(name, new FaceInfo(template));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="template"></param>
        /// <returns><code>name</code> of the best matching face and <code>confidence</code> value [0, 1]</returns>
        public (string name, float confidence) GetBestMatch(byte[] template)
        {
            (string, float) outValue = (string.Empty, 0);

            var matches = _storedFaces.AsParallel().Select(x => (x.Key, x.Value.GetSimilarity(template))).AsEnumerable();
            foreach (var match in matches)
            {
                if (match.Item2 > outValue.Item2)
                {
                    outValue = match;
                }
            }
            return outValue;
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
        private const float WeightMaxMatch = 5;

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
            if (template == null || template.Length != FSDK.TemplateSize)
            {
                throw new ArgumentException($"faceTemplate of length {FSDK.TemplateSize} expected, got length {template?.Length}");
            }
        }

        public float GetSimilarity(byte[] faceTemplate)
        {
            int numMatchedFaces = 0;
            float[] similarities = new float[_faceTemplates.Count];
            Parallel.For(0, _faceTemplates.Count, (i) =>
            {
                var ithTemplate = _faceTemplates[i];
                if (FSDK.FSDKE_OK == FSDK.MatchFaces(ref faceTemplate, ref ithTemplate, ref similarities[i]))
                {
                    Interlocked.Increment(ref numMatchedFaces);
                }
            });

            return similarities.Max();
        }
       
    }
}