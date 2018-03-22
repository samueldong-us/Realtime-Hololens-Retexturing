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
using Realtime_Hololens_Retexturing.Content;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Numerics;

namespace Realtime_Hololens_Retexturing.Common
{
    internal class Model : Disposer
    {
        private SharpDX.Direct3D11.Buffer VertexBuffer;
        private SharpDX.Direct3D11.Buffer ModelConstantBuffer;

        public Vector3 Position
        {
            get => _Position;
            set
            {
                _Position = value;
                UpdateTransform();
            }
        }

        public Vector3 Rotation
        {
            get => _Rotation;
            set
            {
                _Rotation = value;
                UpdateTransform();
            }
        }

        private Vector3 _Position;
        private Vector3 _Rotation;
        private int VertexCount;
        private ModelConstantBuffer ModelConstantBufferData = new ModelConstantBuffer();
        private DeviceResources DeviceResources;

        public static InputLayout GetInputLayout(SharpDX.Direct3D11.Device device, byte[] shaderBytecode)
        {
            InputElement[] inputDescription =
            {
                new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0, InputClassification.PerVertexData, 0),
                new InputElement("NORMAL", 0, Format.R32G32B32_Float, 12, 0, InputClassification.PerVertexData, 0),
                new InputElement("TEXCOORD", 0, Format.R32G32_Float, 24, 0, InputClassification.PerVertexData, 0)
            };
            return new InputLayout(device, shaderBytecode, inputDescription);
        }

        public Model(DeviceResources deviceResources, VertexPositionNormalUV[] mesh)
        {
            DeviceResources = deviceResources;
            CreateDeviceDependantResources(mesh);
        }

        public void Render(Action<int> drawingFunction)
        {
            var device = DeviceResources.D3DDevice;
            var context = DeviceResources.D3DDeviceContext;

            var vertexBufferBinding = new VertexBufferBinding(VertexBuffer, SharpDX.Utilities.SizeOf<VertexPositionNormalUV>(), 0);
            context.InputAssembler.SetVertexBuffers(0, vertexBufferBinding);
            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            context.VertexShader.SetConstantBuffer(0, ModelConstantBuffer);

            drawingFunction(VertexCount);
        }

        private void UpdateTransform()
        {
            var modelRotation = Matrix4x4.CreateFromYawPitchRoll(Rotation.X, Rotation.Y, Rotation.Z);
            var modelTranslation = Matrix4x4.CreateTranslation(Position);

            ModelConstantBufferData.Model = Matrix4x4.Transpose(modelRotation * modelTranslation);
            DeviceResources.D3DDeviceContext.UpdateSubresource(ref ModelConstantBufferData, ModelConstantBuffer);
        }

        private void CreateDeviceDependantResources(VertexPositionNormalUV[] mesh)
        {
            var device = DeviceResources.D3DDevice;
            VertexCount = mesh.Length;
            VertexBuffer = ToDispose(SharpDX.Direct3D11.Buffer.Create(device, BindFlags.VertexBuffer, mesh));
            ModelConstantBuffer = ToDispose(SharpDX.Direct3D11.Buffer.Create(device, BindFlags.ConstantBuffer, ref ModelConstantBufferData));
        }
    }
}