// Copyright (C) 2018 The Regents of the University of California (Regents).
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are
// met:
//
//     * Redistributions of source code must retain the above copyright
//       notice, this list of conditions and the following disclaimer.
//
//     * Redistributions in binary form must reproduce the above
//       copyright notice, this list of conditions and the following
//       disclaimer in the documentation and/or other materials provided
//       with the distribution.
//
//     * Neither the name of The Regents or University of California nor the
//       names of its contributors may be used to endorse or promote products
//       derived from this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
// ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDERS OR CONTRIBUTORS BE
// LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
// SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
// INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
// ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
// POSSIBILITY OF SUCH DAMAGE.
//
// Please contact the author of this library if you have any questions.
// Author: Samuel Dong (samuel_dong@umail.ucsb.edu)
using System.Numerics;
using System.Reflection;

namespace Realtime_Hololens_Retexturing.Content
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