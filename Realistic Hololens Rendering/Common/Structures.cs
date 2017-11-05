using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Realistic_Hololens_Rendering.Common
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
