using SharpDX.Direct3D;
using SharpDX.Direct3D11;

namespace Realistic_Hololens_Rendering.Common
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