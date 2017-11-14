using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Face;
using Luxand;

namespace LuxandFaceLib
{
    public class LuxandFaceInfo : IFaceInfo<byte[]>
    {
        public ICollection<byte[]> Templates => _faceTemplates;
        private List<byte[]> _faceTemplates;

        private const float WeightAvgMatch = 1;
        private const float WeightMaxMatch = 5;

        public LuxandFaceInfo(byte[] faceTemplate)
        {
            _faceTemplates = new List<byte[]>{faceTemplate};
        }

        public void Merge(IFaceInfo<byte[]> info)
        {
            _faceTemplates.AddRange(info.Templates);
        }

        public void AddTemplate(byte[] faceTemplate)
        {
            _faceTemplates.Add(faceTemplate);
        }

        public bool IsValid(byte[] template)
        {
            return template != null && template.Length == FSDK.TemplateSize;
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