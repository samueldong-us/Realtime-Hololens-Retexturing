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
using Realtime_Hololens_Retexturing.Common;
using SharpDX.Direct3D11;
using System;
using System.Numerics;

namespace Realtime_Hololens_Retexturing.Content
{
    class TextureDebugRenderer : Disposer
    {
        private SharpDX.Direct3D11.Buffer IndexBuffer;
        private SharpDX.Direct3D11.Buffer VertexBuffer;
        private SharpDX.Direct3D11.Buffer ScreenPositionBuffer;

        private InputLayout InputLayout;
        private PixelShader PixelShader;
        private VertexShader VertexShader;
        private ScreenPositionBuffer ScreenPositionData = new ScreenPositionBuffer();

        private DeviceResources Resources;

        public TextureDebugRenderer(DeviceResources resources)
        {
            Resources = resources;
            CreateDeviceDependentResourcesAsync();
        }

        public void Render(ShaderResourceView texture, Vector4 bounds)
        {
            var device = Resources.D3DDevice;
            var context = Resources.D3DDeviceContext;

            ScreenPositionData.Bounds = bounds;
            context.UpdateSubresource(ref ScreenPositionData, ScreenPositionBuffer);

            context.VertexShader.Set(VertexShader);
            context.VertexShader.SetConstantBuffer(0, ScreenPositionBuffer);
            context.GeometryShader.Set(null);
            context.PixelShader.Set(PixelShader);
            context.PixelShader.SetShaderResource(0, texture);

            int stride = SharpDX.Utilities.SizeOf<VertexPositionUV>();
            int offset = 0;
            var bufferBinding = new VertexBufferBinding(VertexBuffer, stride, offset);
            context.InputAssembler.SetVertexBuffers(0, bufferBinding);
            context.InputAssembler.SetIndexBuffer(IndexBuffer, SharpDX.DXGI.Format.R16_UInt, 0);
            context.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;
            context.InputAssembler.InputLayout = InputLayout;

            context.DrawIndexedInstanced(6, 2, 0, 0, 0);

            context.PixelShader.SetShaderResource(0, null);
        }

        public async void CreateDeviceDependentResourcesAsync()
        {
            var device = Resources.D3DDevice;
            var folder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            VertexShader = ToDispose(await DirectXHelper.LoadShader<VertexShader>(device, folder, @"Content\Shaders\Texture Debug\VertexShader.cso"));

            InputElement[] vertexDescription =
            {
                new InputElement("POSITION", 0, SharpDX.DXGI.Format.R32G32B32_Float, 0, 0, InputClassification.PerVertexData, 0),
                new InputElement("TEXCOORD", 0, SharpDX.DXGI.Format.R32G32_Float, 12, 0, InputClassification.PerVertexData, 0)
            };

            var vertexShaderBytecode = await DirectXHelper.ReadDataAsync(await folder.GetFileAsync(@"Content\Shaders\Texture Debug\VertexShader.cso"));
            InputLayout = ToDispose(new InputLayout(device, vertexShaderBytecode, vertexDescription));

            PixelShader = ToDispose(await DirectXHelper.LoadShader<PixelShader>(device, folder, @"Content\Shaders\Texture Debug\PixelShader.cso"));

            VertexPositionUV[] vertices =
            {
                new VertexPositionUV(new Vector3(0.0f, 0.0f, 0.0f), new Vector2(0.0f, 0.0f)),
                new VertexPositionUV(new Vector3(1.0f, 0.0f, 0.0f), new Vector2(1.0f, 0.0f)),
                new VertexPositionUV(new Vector3(1.0f, 1.0f, 0.0f), new Vector2(1.0f, 1.0f)),
                new VertexPositionUV(new Vector3(0.0f, 1.0f, 0.0f), new Vector2(0.0f, 1.0f))
            };
            VertexBuffer = ToDispose(SharpDX.Direct3D11.Buffer.Create(device, BindFlags.VertexBuffer, vertices));

            ushort[] indices = { 0, 1, 2, 2, 3, 0 };
            IndexBuffer = ToDispose(SharpDX.Direct3D11.Buffer.Create(device, BindFlags.IndexBuffer, indices));

            ScreenPositionBuffer = ToDispose(SharpDX.Direct3D11.Buffer.Create(device, BindFlags.ConstantBuffer, ref ScreenPositionData));
        }
    }
}