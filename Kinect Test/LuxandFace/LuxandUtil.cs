using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Luxand;

namespace LuxandFace
{
    internal static class LuxandUtil
    {
        public static (Rectangle rect, double rotAngle) TFacePositionToRectRotAngle(FSDK.TFacePosition tfp)
        {
            return (new Rectangle(tfp.xc - tfp.w, tfp.yc - tfp.w, tfp.w, tfp.w), tfp.angle);
        }

        internal static FSDK.TFacePosition RectRotAngleToTFacePosition(Rectangle rect, double rotAngle)
        {
            return new FSDK.TFacePosition()
            {
                angle = rotAngle,
                w = rect.Width,
                xc = rect.X + rect.Width / 2,
                yc = rect.Y + rect.Width / 2
            };
        }
    }


}
