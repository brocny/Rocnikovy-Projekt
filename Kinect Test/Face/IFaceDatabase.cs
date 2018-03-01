using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Face
{
    interface IFaceDatabase<T>
    {
        T GetFaceInfo(int id);
        string GetName(int id);

    }
}
