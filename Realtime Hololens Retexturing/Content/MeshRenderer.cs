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
using System.Threading.Tasks;

namespace Realtime_Hololens_Retexturing.Content
{
    internal class MeshRenderer : Disposer
    {
        #region DirectX Objects

        private SharpDX.Direct3D11.Buffer LayoutConstantBuffer;
        private LayoutConstantBuffer LayoutData = new LayoutConstantBuffer();
        private GeometryShader RenderGeometryShader;
        private PixelShader RenderPixelShader;
        private VertexShader RenderVertexShader;

        #endregion DirectX Objects

        private DeviceResources Resources;

        public MeshRenderer(DeviceResources resources)
        {
            Resources = resources;
        }

        public async Task CreateDeviceDependantResources()
        {
            var device = Resources.D3DDevice;
            var folder = Windows.ApplicationModel.Package.Current.InstalledLocation;

            RenderVertexShader = ToDispose(await DirectXHelper.LoadShader<VertexShader>(device, folder, @"Content\Shaders\Mesh Rendering\VertexShader.cso"));
            RenderGeometryShader = ToDispose(await DirectXHelper.LoadShader<GeometryShader>(device, folder, @"Content\Shaders\Mesh Rendering\GeometryShader.cso"));
            RenderPixelShader = ToDispose(await DirectXHelper.LoadShader<PixelShader>(device, folder, @"Content\Shaders\Mesh Rendering\PixelShader.cso"));

            LayoutConstantBuffer = ToDispose(SharpDX.Direct3D11.Buffer.Create(device, BindFlags.ConstantBuffer, ref LayoutData));
        }

        public void RenderMesh(MeshCollection meshes, ShaderResourceView texture)
        {
            var device = Resources.D3DDevice;
            var context = Resources.D3DDeviceContext;

            var newTriangleCount = meshes.TotalNumberOfTriangles;
            var newNumberOfSide = (int)Math.Ceiling(Math.Sqrt(newTriangleCount / 2.0));

            context.VertexShader.Set(RenderVertexShader);
            context.GeometryShader.Set(RenderGeometryShader);
            context.GeometryShader.SetConstantBuffer(3, LayoutConstantBuffer);
            context.PixelShader.Set(RenderPixelShader);
            context.PixelShader.SetShaderResource(0, texture);
            context.PixelShader.SetSampler(0, new SamplerState(device, new SamplerStateDescription
            {
                AddressU = TextureAddressMode.Clamp,
                AddressV = TextureAddressMode.Clamp,
                AddressW = TextureAddressMode.Clamp,
                ComparisonFunction = Comparison.Equal,
                Filter = Filter.MinMagLinearMipPoint
            }));

            context.Rasterizer.State = new RasterizerState(device, new RasterizerStateDescription
            {
                CullMode = CullMode.None,
                FillMode = FillMode.Solid
            });

            int newOffset = 0;
            meshes.Draw(numberOfIndices =>
            {
                context.DrawIndexedInstanced(numberOfIndices, 2, 0, 0, 0);
            },
            (guid, numberOfIndices) =>
            {
                LayoutData.Offset = (uint)newOffset;
                LayoutData.Size = (uint)newNumberOfSide;
                newOffset += numberOfIndices / 3;
                context.UpdateSubresource(ref LayoutData, LayoutConstantBuffer);
                return true;
            });

            context.PixelShader.SetShaderResource(0, null);
        }
    }
}