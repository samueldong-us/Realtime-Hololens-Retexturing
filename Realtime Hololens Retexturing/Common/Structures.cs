using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Realtime_Hololens_Retexturing.Common
{
    public class Face
    {
        public Vertex[] Vertices = new Vertex[3];
    }

    public class Vertex
    {
        public int PositionIndex;
        public int NormalIndex;
        public int UVIndex;
    }
}
