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
using System;
using System.Diagnostics;
using System.Numerics;
using Windows.UI.Input.Spatial;

namespace Realtime_Hololens_Retexturing.Content
{
    internal class CameraTestRenderer : Disposer
    {
        #region Dependent Objects

        private DeviceResources deviceResources;
        private PhysicalCamera physicalCamera;

        #endregion Dependent Objects

        #region DirectX Objects
        
        private SharpDX.Direct3D11.Buffer indexBuffer;
        private SharpDX.Direct3D11.InputLayout inputLayout;
        private SharpDX.Direct3D11.Buffer modelConstantBuffer;
        private SharpDX.Direct3D11.PixelShader pixelShader;
        private SharpDX.Direct3D11.Buffer vertexBuffer;
        private SharpDX.Direct3D11.VertexShader vertexShader;

        #endregion DirectX Objects

        private bool loadingFinished;
        private ModelConstantBuffer modelConstantBufferData = new ModelConstantBuffer();
        private Vector3 position = new Vector3(0.0f, 0.0f, -2.0f);
        private Vector3 headPosition = new Vector3(0.0f, 0.0f, 0.0f);
        private Vector3 headForward = new Vector3(0.0f, 0.0f, -1.0f);

        public CameraTestRenderer(DeviceResources deviceResources, PhysicalCamera physicalCamera)
        {
            this.deviceResources = deviceResources;
            this.physicalCamera = physicalCamera;

            CreateDeviceDependentResourcesAsync();
        }

        public async void CreateDeviceDependentResourcesAsync()
        {
            ReleaseDeviceDependentResources();

            var folder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            var vertexShaderFilename = @"Content\Shaders\Camera Testing\VprtVertexShader.cso";
            var vertexShaderBytecode = await DirectXHelper.ReadDataAsync(await folder.GetFileAsync(vertexShaderFilename));
            vertexShader = ToDispose(new SharpDX.Direct3D11.VertexShader(deviceResources.D3DDevice, vertexShaderBytecode));

            SharpDX.Direct3D11.InputElement[] vertexDescription =
            {
                new SharpDX.Direct3D11.InputElement("POSITION", 0, SharpDX.DXGI.Format.R32G32B32_Float, 0, 0, SharpDX.Direct3D11.InputClassification.PerVertexData, 0),
                new SharpDX.Direct3D11.InputElement("TEXCOORD", 0, SharpDX.DXGI.Format.R32G32_Float, 12, 0, SharpDX.Direct3D11.InputClassification.PerVertexData, 0)
            };

            inputLayout = ToDispose(new SharpDX.Direct3D11.InputLayout(deviceResources.D3DDevice, vertexShaderBytecode, vertexDescription));

            var pixelShaderBytecode = await DirectXHelper.ReadDataAsync(await folder.GetFileAsync(@"Content\Shaders\Camera Testing\PixelShader.cso"));
            pixelShader = ToDispose(new SharpDX.Direct3D11.PixelShader(deviceResources.D3DDevice, pixelShaderBytecode));

            VertexPositionUV[] vertices =
            {
                new VertexPositionUV(new Vector3(-0.2f, -0.2f, 0.0f), new Vector2(0.0f, 0.0f)),
                new VertexPositionUV(new Vector3(0.2f, -0.2f, 0.0f), new Vector2(1.0f, 0.0f)),
                new VertexPositionUV(new Vector3(0.2f, 0.2f, 0.0f), new Vector2(1.0f, 1.0f)),
                new VertexPositionUV(new Vector3(-0.2f, 0.2f, 0.0f), new Vector2(0.0f, 1.0f))
            };
            vertexBuffer = ToDispose(SharpDX.Direct3D11.Buffer.Create(deviceResources.D3DDevice, SharpDX.Direct3D11.BindFlags.VertexBuffer, vertices));

            ushort[] indices = { 0, 1, 2, 2, 3, 0 };
            indexBuffer = ToDispose(SharpDX.Direct3D11.Buffer.Create(deviceResources.D3DDevice, SharpDX.Direct3D11.BindFlags.IndexBuffer, indices));

            modelConstantBuffer = ToDispose(SharpDX.Direct3D11.Buffer.Create(deviceResources.D3DDevice, SharpDX.Direct3D11.BindFlags.ConstantBuffer, ref modelConstantBufferData));

            loadingFinished = true;
        }

        public void PositionHologram(SpatialPointerPose pointerPose)
        {
            if (pointerPose != null)
            {
                headPosition = pointerPose.Head.Position;
                headForward = pointerPose.Head.ForwardDirection;
                position = headPosition + 2.0f * headForward;
            }
        }

        public void ReleaseDeviceDependentResources()
        {
            loadingFinished = false;
            RemoveAndDispose(ref vertexShader);
            RemoveAndDispose(ref inputLayout);
            RemoveAndDispose(ref pixelShader);
            RemoveAndDispose(ref modelConstantBuffer);
            RemoveAndDispose(ref vertexBuffer);
            RemoveAndDispose(ref indexBuffer);
        }

        public void Update(StepTimer timer)
        {
            Matrix4x4 modelRotation = Matrix4x4.CreateConstrainedBillboard(position, headPosition, Vector3.UnitY, headForward, Vector3.UnitZ);
            Matrix4x4 modelTranslation = Matrix4x4.CreateTranslation(position);
            modelConstantBufferData.Model = Matrix4x4.Transpose(modelRotation * modelTranslation);

            if (!loadingFinished)
                return;

            var context = deviceResources.D3DDeviceContext;
            context.UpdateSubresource(ref modelConstantBufferData, modelConstantBuffer);
        }

        public void Render()
        {
            if (!loadingFinished || !physicalCamera.Ready)
                return;

            var device = deviceResources.D3DDevice;
            var context = deviceResources.D3DDeviceContext;
            int stride = SharpDX.Utilities.SizeOf<VertexPositionUV>();
            int offset = 0;
            var bufferBinding = new SharpDX.Direct3D11.VertexBufferBinding(vertexBuffer, stride, offset);
            context.InputAssembler.SetVertexBuffers(0, bufferBinding);
            context.InputAssembler.SetIndexBuffer(indexBuffer, SharpDX.DXGI.Format.R16_UInt, 0);
            context.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;
            context.InputAssembler.InputLayout = inputLayout;

            context.VertexShader.SetShader(vertexShader, null, 0);
            context.VertexShader.SetConstantBuffers(0, modelConstantBuffer);
            context.PixelShader.SetShader(pixelShader, null, 0);
            var cameraTexture = physicalCamera.AcquireTexture();
            if (cameraTexture == null)
                return;
            var luminanceView = new SharpDX.Direct3D11.ShaderResourceView(device, cameraTexture, new SharpDX.Direct3D11.ShaderResourceViewDescription()
            {
                Format = SharpDX.DXGI.Format.R8_UInt,
                Dimension = SharpDX.Direct3D.ShaderResourceViewDimension.Texture2D,
                Texture2D = new SharpDX.Direct3D11.ShaderResourceViewDescription.Texture2DResource()
                {
                    MipLevels = 1
                }
            });
            var chrominanceView = new SharpDX.Direct3D11.ShaderResourceView(device, cameraTexture, new SharpDX.Direct3D11.ShaderResourceViewDescription()
            {
                Format = SharpDX.DXGI.Format.R8G8_UInt,
                Dimension = SharpDX.Direct3D.ShaderResourceViewDimension.Texture2D,
                Texture2D = new SharpDX.Direct3D11.ShaderResourceViewDescription.Texture2DResource()
                {
                    MipLevels = 1
                }
            });
            context.PixelShader.SetShaderResource(0, luminanceView);
            context.PixelShader.SetShaderResource(1, chrominanceView);

            context.DrawIndexedInstanced(6, 2, 0, 0, 0);

            luminanceView.Dispose();
            chrominanceView.Dispose();
            physicalCamera.ReleaseTexture();
        }
    }
}