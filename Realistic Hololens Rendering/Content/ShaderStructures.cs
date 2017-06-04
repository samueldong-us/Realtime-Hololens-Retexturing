using System.Numerics;

namespace Realistic_Hololens_Rendering.Content
{
    /// <summary>
    /// Constant buffer used to send hologram position transform to the shader pipeline.
    /// </summary>
    internal struct ModelConstantBuffer
    {
        public Matrix4x4 model;
    }

    internal struct TransformConstantBuffer
    {
        public Matrix4x4 VertexTransform;
        public Matrix4x4 NormalTransform;
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

    internal struct VertexPositionUv
    {
        public Vector3 position;
        public Vector2 uv;

        public VertexPositionUv(Vector3 position, Vector2 uv)
        {
            this.position = position;
            this.uv = uv;
        }
    }
}