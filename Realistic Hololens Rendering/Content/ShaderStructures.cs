using System.Numerics;
using System.Reflection;

namespace Realistic_Hololens_Rendering.Content
{
    /// <summary>
    /// Constant buffer used to send hologram position transform to the shader pipeline.
    /// </summary>
    internal struct ModelConstantBuffer
    {
        public Matrix4x4 Model;
    }

    internal struct ScreenPositionBuffer
    {
        public Vector4 Bounds;
    }

    internal struct TransformConstantBuffer
    {
        public Matrix4x4 VertexTransform;
        public Matrix4x4 NormalTransform;
    }

    internal struct CameraConstantBuffer
    {
        public Matrix4x4 ViewProjection;
    }

    internal struct Vector4ConstantBuffer
    {
        public Vector4 Vector;
    }

    internal struct UpdateLayoutConstantBuffer
    {
        public uint OldOffset;
        public uint NewOffset;
        public uint OldSize;
        public uint NewSize;
    }

    internal struct LayoutConstantBuffer
    {
        public uint Offset;
        public uint Size;
        public long Padding;
    }

    internal struct CubeArrayBuffer
    {
        public Matrix4x4 ViewProjection0;
        public Matrix4x4 ViewProjection1;
        public Matrix4x4 ViewProjection2;
        public Matrix4x4 ViewProjection3;
        public Matrix4x4 ViewProjection4;
        public Matrix4x4 ViewProjection5;

        public Matrix4x4 this[int index]
        {
            set
            {
                switch (index)
                {
                    case 0: ViewProjection0 = value; break;
                    case 1: ViewProjection1 = value; break;
                    case 2: ViewProjection2 = value; break;
                    case 3: ViewProjection3 = value; break;
                    case 4: ViewProjection4 = value; break;
                    case 5: ViewProjection5 = value; break;
                }
            }
        }
    }

    /// <summary>
    /// Used to send per-vertex data to the vertex shader.
    /// </summary>
    internal struct VertexPositionColor
    {
        public VertexPositionColor(Vector3 pos, Vector3 color)
        {
            this.pos   = pos;
            this.color = color;
        }

        public Vector3 pos;
        public Vector3 color;
    }

    internal struct VertexPositionUV
    {
        public Vector3 position;
        public Vector2 uv;

        public VertexPositionUV(Vector3 position, Vector2 uv)
        {
            this.position = position;
            this.uv = uv;
        }
    }

    internal struct VertexPositionNormalUV
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 UV;
    }
}