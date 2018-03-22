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
using SharpDX.Direct3D;
using SharpDX.Direct3D11;

namespace Realtime_Hololens_Retexturing.Common
{
    class MeshTextureSet : Disposer
    {
        public RenderTargetView RenderColorView { get; private set; }
        public RenderTargetView RenderQualityAndTimeView { get; private set; }
        public ShaderResourceView ColorResourceView { get; private set; }
        public ShaderResourceView QualityAndTimeResourceView { get; private set; }
        public Texture2D MeshColor { get; private set; }
        public Texture2D MeshQualityAndTime { get; private set; }

        private DeviceResources Resources;
        private int Resolution;

        public MeshTextureSet(DeviceResources resources, int resolution)
        {
            Resources = resources;
            Resolution = resolution;
        }

        public void Initialize()
        {
            CreateDeviceDependentResources();
        }

        public void CreateDeviceDependentResources()
        {
            var device = Resources.D3DDevice;

            MeshColor = ToDispose(new Texture2D(device, new Texture2DDescription()
            {
                Width = Resolution,
                Height = Resolution,
                ArraySize = 1,
                CpuAccessFlags = CpuAccessFlags.Read,
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                Format = SharpDX.DXGI.Format.R8G8B8A8_UNorm,
                Usage = ResourceUsage.Default,
                OptionFlags = ResourceOptionFlags.None,
                MipLevels = 1,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0)
            }));

            MeshQualityAndTime = ToDispose(new Texture2D(device, new Texture2DDescription()
            {
                Width = Resolution,
                Height = Resolution,
                ArraySize = 1,
                CpuAccessFlags = CpuAccessFlags.None,
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                Format = SharpDX.DXGI.Format.R16G16_Float,
                Usage = ResourceUsage.Default,
                OptionFlags = ResourceOptionFlags.None,
                MipLevels = 1,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0)
            }));

            var colorRenderTargetViewDescription = new RenderTargetViewDescription()
            {
                Format = MeshColor.Description.Format,
                Dimension = RenderTargetViewDimension.Texture2D
            };
            colorRenderTargetViewDescription.Texture2D.MipSlice = 0;
            RenderColorView = ToDispose(new RenderTargetView(device, MeshColor, colorRenderTargetViewDescription));

            var qualityAndTimeRenderTargetViewDescription = new RenderTargetViewDescription()
            {
                Format = MeshQualityAndTime.Description.Format,
                Dimension = RenderTargetViewDimension.Texture2D
            };
            qualityAndTimeRenderTargetViewDescription.Texture2D.MipSlice = 0;
            RenderQualityAndTimeView = ToDispose(new RenderTargetView(device, MeshQualityAndTime, qualityAndTimeRenderTargetViewDescription));

            var colorShaderResourceViewDescription = new ShaderResourceViewDescription()
            {
                Format = MeshColor.Description.Format,
                Dimension = ShaderResourceViewDimension.Texture2D
            };
            colorShaderResourceViewDescription.Texture2D.MipLevels = -1;
            colorShaderResourceViewDescription.Texture2D.MostDetailedMip = 0;
            ColorResourceView = ToDispose(new ShaderResourceView(device, MeshColor, colorShaderResourceViewDescription));

            var qualityAndTimeShaderResourceViewDescription = new ShaderResourceViewDescription()
            {
                Format = MeshQualityAndTime.Description.Format,
                Dimension = ShaderResourceViewDimension.Texture2D
            };
            qualityAndTimeShaderResourceViewDescription.Texture2D.MipLevels = -1;
            qualityAndTimeShaderResourceViewDescription.Texture2D.MostDetailedMip = 0;
            QualityAndTimeResourceView = ToDispose(new ShaderResourceView(device, MeshQualityAndTime, qualityAndTimeShaderResourceViewDescription));
        }
    }
}