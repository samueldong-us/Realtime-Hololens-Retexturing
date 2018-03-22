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
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using System;
using System.Collections.Generic;
using System.Numerics;
using Windows.Perception.Spatial;

namespace Realtime_Hololens_Retexturing.Content
{
    internal class MeshTexturer : Disposer
    {
        #region DirectX Objects

        private SharpDX.Direct3D11.Buffer CameraConstantBuffer;
        private ShaderResourceView DepthResource;
        private DepthStencilView DepthTarget;
        private Texture2D DepthTexture;
        private SharpDX.Direct3D11.Buffer LayoutConstantBuffer;
        private PixelShader PrepassPixelShader;
        private VertexShader PrepassVertexShader;
        private GeometryShader ProjectionGeometryShader;
        private PixelShader ProjectionPixelShader;
        private VertexShader ProjectionVertexShader;
        private GeometryShader UpdateGeometryShader;
        private SharpDX.Direct3D11.Buffer UpdateLayoutConstantBuffer;
        private PixelShader UpdatePixelShader;
        private VertexShader UpdateVertexShader;

        #endregion DirectX Objects

        private readonly int Resolution;
        private bool Active;
        private PhysicalCamera Camera;
        private CameraConstantBuffer CameraData = new CameraConstantBuffer();
        private int CurrentTexture;
        private LayoutConstantBuffer LayoutData = new LayoutConstantBuffer();
        private MeshTextureSet[] MeshTextures;
        private DeviceResources Resources;
        private UpdateLayoutConstantBuffer UpdateLayoutData = new UpdateLayoutConstantBuffer();

        public MeshTexturer(DeviceResources resources, PhysicalCamera camera, int resolution)
        {
            Resources = resources;
            Camera = camera;
            Resolution = resolution;
            Active = false;

            MeshTextures = new[]
            {
                ToDispose(new MeshTextureSet(resources, Resolution)),
                ToDispose(new MeshTextureSet(resources, Resolution))
            };
            CurrentTexture = 0;

            CreateDeviceDependantResources();
        }

        public ShaderResourceView ColorResourceView => MeshTextures[CurrentTexture].ColorResourceView;
        public Texture2D MeshColorTexture => MeshTextures[CurrentTexture].MeshColor;
        public Texture2D MeshQualityAndTimeTexture => MeshTextures[CurrentTexture].MeshQualityAndTime;
        public ShaderResourceView QualityAndTimeResourceView => MeshTextures[CurrentTexture].QualityAndTimeResourceView;
        public RenderTargetView RenderColorView => MeshTextures[CurrentTexture].RenderColorView;
        public RenderTargetView RenderQualityAndTimeView => MeshTextures[CurrentTexture].RenderQualityAndTimeView;

        public void InitializeTextures()
        {
            foreach (var meshTexture in MeshTextures)
            {
                meshTexture.Initialize();
            }
        }

        public void ProjectCameraTexture(MeshCollection meshes, SpatialCoordinateSystem coordinateSystem)
        {
            if (!Active)
                return;

            PerformPrepass(meshes, coordinateSystem);
            PerformProjection(meshes);
        }

        public void UpdatePacking(MeshCollection meshes, int previousCount, Dictionary<Guid, int> previousOffsets)
        {
            if (!Active)
                return;

            var nextTexture = (CurrentTexture + 1) % 2;

            var oldTriangleCount = previousCount;
            var oldNumberOnSide = (int)Math.Ceiling(Math.Sqrt(oldTriangleCount / 2.0));

            var newTriangleCount = meshes.TotalNumberOfTriangles;
            var newNumberOfSide = (int)Math.Ceiling(Math.Sqrt(newTriangleCount / 2.0));

            var device = Resources.D3DDevice;
            var context = Resources.D3DDeviceContext;

            var currentTextureSet = MeshTextures[CurrentTexture];
            var nextTextureSet = MeshTextures[nextTexture];

            context.VertexShader.Set(UpdateVertexShader);
            context.GeometryShader.Set(UpdateGeometryShader);
            context.GeometryShader.SetConstantBuffer(2, UpdateLayoutConstantBuffer);
            context.PixelShader.Set(UpdatePixelShader);
            context.PixelShader.SetShaderResource(0, currentTextureSet.ColorResourceView);
            context.PixelShader.SetShaderResource(1, currentTextureSet.QualityAndTimeResourceView);

            context.ClearRenderTargetView(nextTextureSet.RenderColorView, new RawColor4(0.0f, 0.0f, 0.0f, 0.0f));
            context.ClearRenderTargetView(nextTextureSet.RenderQualityAndTimeView, new RawColor4(0.0f, 0.0f, 0.0f, 0.0f));
            context.OutputMerger.SetRenderTargets(null, nextTextureSet.RenderColorView);

            context.Rasterizer.SetViewport(0.0f, 0.0f, Resolution, Resolution);

            int newOffset = 0;
            meshes.Draw(numberOfIndices =>
            {
                context.DrawIndexed(numberOfIndices, 0, 0);
            },
            (guid, numberOfIndices) =>
            {
                UpdateLayoutData.OldSize = (uint)oldNumberOnSide;
                UpdateLayoutData.NewOffset = (uint)newOffset;
                UpdateLayoutData.NewSize = (uint)newNumberOfSide;
                newOffset += numberOfIndices / 3;

                if (!previousOffsets.ContainsKey(guid))
                    return false;

                UpdateLayoutData.OldOffset = (uint)previousOffsets[guid];

                context.UpdateSubresource(ref UpdateLayoutData, UpdateLayoutConstantBuffer);
                return true;
            });
            context.PixelShader.SetShaderResource(0, null);
            context.PixelShader.SetShaderResource(1, null);

            context.OutputMerger.SetRenderTargets(null, (RenderTargetView)null);

            CurrentTexture = nextTexture;
        }

        private async void CreateDeviceDependantResources()
        {
            var device = Resources.D3DDevice;
            var folder = Windows.ApplicationModel.Package.Current.InstalledLocation;

            UpdateVertexShader = ToDispose(await DirectXHelper.LoadShader<VertexShader>(device, folder, @"Content\Shaders\Mesh Updating\VertexShader.cso"));
            UpdateGeometryShader = ToDispose(await DirectXHelper.LoadShader<GeometryShader>(device, folder, @"Content\Shaders\Mesh Updating\GeometryShader.cso"));
            UpdatePixelShader = ToDispose(await DirectXHelper.LoadShader<PixelShader>(device, folder, @"Content\Shaders\Mesh Updating\PixelShader.cso"));

            PrepassVertexShader = ToDispose(await DirectXHelper.LoadShader<VertexShader>(device, folder, @"Content\Shaders\Mesh Projection\DepthPrepassVertexShader.cso"));
            PrepassPixelShader = ToDispose(await DirectXHelper.LoadShader<PixelShader>(device, folder, @"Content\Shaders\Mesh Projection\DepthPrepassPixelShader.cso"));

            ProjectionVertexShader = ToDispose(await DirectXHelper.LoadShader<VertexShader>(device, folder, @"Content\Shaders\Mesh Projection\VertexShader.cso"));
            ProjectionGeometryShader = ToDispose(await DirectXHelper.LoadShader<GeometryShader>(device, folder, @"Content\Shaders\Mesh Projection\GeometryShader.cso"));
            ProjectionPixelShader = ToDispose(await DirectXHelper.LoadShader<PixelShader>(device, folder, @"Content\Shaders\Mesh Projection\PixelShader.cso"));

            UpdateLayoutConstantBuffer = ToDispose(SharpDX.Direct3D11.Buffer.Create(device, BindFlags.ConstantBuffer, ref UpdateLayoutData));
            LayoutConstantBuffer = ToDispose(SharpDX.Direct3D11.Buffer.Create(device, BindFlags.ConstantBuffer, ref LayoutData));
            CameraConstantBuffer = ToDispose(SharpDX.Direct3D11.Buffer.Create(device, BindFlags.ConstantBuffer, ref CameraData));

            CreatePrepassDepthResources();

            Active = true;
        }

        private void CreatePrepassDepthResources()
        {
            var device = Resources.D3DDevice;

            DepthTexture = ToDispose(new Texture2D(device, new Texture2DDescription
            {
                Width = Camera.Width,
                Height = Camera.Height,
                ArraySize = 1,
                CpuAccessFlags = CpuAccessFlags.None,
                BindFlags = BindFlags.DepthStencil | BindFlags.ShaderResource,
                Format = Format.R32_Typeless,
                Usage = ResourceUsage.Default,
                OptionFlags = ResourceOptionFlags.None,
                MipLevels = 0,
                SampleDescription = new SampleDescription(1, 0)
            }));
            var depthTargetDescription = new DepthStencilViewDescription
            {
                Format = Format.D32_Float,
                Dimension = DepthStencilViewDimension.Texture2D,
                Flags = DepthStencilViewFlags.None
            };
            depthTargetDescription.Texture2D.MipSlice = 0;
            DepthTarget = ToDispose(new DepthStencilView(device, DepthTexture, depthTargetDescription));
            var depthResourceDescription = new ShaderResourceViewDescription
            {
                Format = Format.R32_Float,
                Dimension = ShaderResourceViewDimension.Texture2D
            };
            depthResourceDescription.Texture2D.MipLevels = -1;
            depthResourceDescription.Texture2D.MostDetailedMip = 0;
            DepthResource = ToDispose(new ShaderResourceView(device, DepthTexture, depthResourceDescription));
        }

        private void PerformPrepass(MeshCollection meshes, SpatialCoordinateSystem coordinateSystem)
        {
            var device = Resources.D3DDevice;
            var context = Resources.D3DDeviceContext;

            CameraData.ViewProjection = Matrix4x4.Transpose(Camera.GetWorldToCameraMatrix(coordinateSystem));
            context.UpdateSubresource(ref CameraData, CameraConstantBuffer);

            context.VertexShader.Set(PrepassVertexShader);
            context.VertexShader.SetConstantBuffer(2, CameraConstantBuffer);
            context.GeometryShader.Set(null);
            context.PixelShader.Set(null);

            context.ClearDepthStencilView(DepthTarget, DepthStencilClearFlags.Depth, 1.0f, 0);
            context.OutputMerger.SetRenderTargets(DepthTarget, (RenderTargetView)null);

            context.Rasterizer.SetViewport(0, 0, Camera.Width, Camera.Height);
            context.Rasterizer.State = new RasterizerState(device, new RasterizerStateDescription
            {
                CullMode = CullMode.None,
                FillMode = FillMode.Solid
            });

            meshes.Draw(numberOfIndices =>
            {
                context.DrawIndexed(numberOfIndices, 0, 0);
            });

            context.OutputMerger.SetRenderTargets(null, (RenderTargetView)null);
        }

        private void PerformProjection(MeshCollection meshes)
        {
            var device = Resources.D3DDevice;
            var context = Resources.D3DDeviceContext;

            var newTriangleCount = meshes.TotalNumberOfTriangles;
            var newNumberOfSide = (int)Math.Ceiling(Math.Sqrt(newTriangleCount / 2.0));

            context.VertexShader.Set(ProjectionVertexShader);
            context.GeometryShader.Set(ProjectionGeometryShader);
            context.GeometryShader.SetConstantBuffer(3, LayoutConstantBuffer);
            context.PixelShader.Set(ProjectionPixelShader);
            context.PixelShader.SetConstantBuffer(2, CameraConstantBuffer);
            var cameraTexture = Camera.AcquireTexture();
            if (cameraTexture == null)
            {
                return;
            }
            var luminanceView = new ShaderResourceView(device, cameraTexture, new ShaderResourceViewDescription
            {
                Format = Format.R8_UInt,
                Dimension = ShaderResourceViewDimension.Texture2D,
                Texture2D = new ShaderResourceViewDescription.Texture2DResource()
                {
                    MipLevels = 1
                }
            });
            var chrominanceView = new ShaderResourceView(device, cameraTexture, new ShaderResourceViewDescription
            {
                Format = Format.R8G8_UInt,
                Dimension = ShaderResourceViewDimension.Texture2D,
                Texture2D = new ShaderResourceViewDescription.Texture2DResource()
                {
                    MipLevels = 1
                }
            });
            context.PixelShader.SetShaderResource(1, luminanceView);
            context.PixelShader.SetShaderResource(2, chrominanceView);
            context.PixelShader.SetShaderResource(3, DepthResource);

            context.OutputMerger.SetRenderTargets(null, MeshTextures[CurrentTexture].RenderColorView);

            context.Rasterizer.SetViewport(0, 0, Resolution, Resolution);
            context.Rasterizer.State = new RasterizerState(device, new RasterizerStateDescription
            {
                CullMode = CullMode.None,
                FillMode = FillMode.Solid
            });

            int newOffset = 0;
            meshes.Draw(numberOfIndices =>
            {
                context.DrawIndexed(numberOfIndices, 0, 0);
            },
            (guid, numberOfIndices) =>
            {
                LayoutData.Offset = (uint)newOffset;
                LayoutData.Size = (uint)newNumberOfSide;
                newOffset += numberOfIndices / 3;
                context.UpdateSubresource(ref LayoutData, LayoutConstantBuffer);
                return true;
            });

            context.PixelShader.SetShaderResource(1, null);
            context.PixelShader.SetShaderResource(2, null);
            context.PixelShader.SetShaderResource(3, null);

            context.OutputMerger.SetRenderTargets(null, (RenderTargetView)null);

            luminanceView.Dispose();
            chrominanceView.Dispose();
            Camera.ReleaseTexture();
        }
    }
}