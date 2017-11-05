using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.WIC;
using System;
using System.IO;
using System.Text;
using Windows.Storage;

namespace Realistic_Hololens_Rendering.Common
{
    static class MeshExporter
    {

        public static void ExportTexture(DeviceResources resources, StorageFile textureFile, Texture2D texture)
        {
            var device = resources.D3DDevice;
            var context = resources.D3DDeviceContext;

            var textureToSave = texture;
            var outputTexture = new Texture2D(device, new Texture2DDescription
            {
                Width = textureToSave.Description.Width,
                Height = textureToSave.Description.Height,
                MipLevels = 1,
                ArraySize = 1,
                Format = textureToSave.Description.Format,
                Usage = ResourceUsage.Staging,
                SampleDescription = new SampleDescription(1, 0),
                BindFlags = BindFlags.None,
                CpuAccessFlags = CpuAccessFlags.Read,
                OptionFlags = ResourceOptionFlags.None
            });

            context.CopyResource(textureToSave, outputTexture);
            var mappedResource = context.MapSubresource(outputTexture, 0, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None, out var dataStream);
            var dataRectangle = new DataRectangle
            {
                DataPointer = dataStream.DataPointer,
                Pitch = mappedResource.RowPitch
            };
            var imagingFactory = new ImagingFactory();
            var bitmap = new Bitmap(imagingFactory, outputTexture.Description.Width, outputTexture.Description.Height, PixelFormat.Format32bppRGBA, dataRectangle);
            using (var stream = new MemoryStream())
            using (var bitmapEncoder = new PngBitmapEncoder(imagingFactory, stream))
            using (var bitmapFrame = new BitmapFrameEncode(bitmapEncoder))
            {
                bitmapFrame.Initialize();
                bitmapFrame.SetSize(bitmap.Size.Width, bitmap.Size.Height);
                var pixelFormat = PixelFormat.FormatDontCare;
                bitmapFrame.SetPixelFormat(ref pixelFormat);
                bitmapFrame.WriteSource(bitmap);
                bitmapFrame.Commit();
                bitmapEncoder.Commit();
                FileIO.WriteBytesAsync(textureFile, stream.ToArray()).AsTask().Wait(-1);
            }
            context.UnmapSubresource(outputTexture, 0);
            outputTexture.Dispose();
            bitmap.Dispose();
        }

        public static string GetMaterialFile(string textureFile)
        {
            var materialBuilder = new StringBuilder();
            materialBuilder
                .AppendLine("newmtl default")
                .AppendLine("Ka 0.0 0.0 0.0")
                .AppendLine("Kd 1.0 1.0 1.0")
                .AppendLine("Ks 0.0 0.0 0.0")
                .AppendLine("d 1.0")
                .AppendLine("illum 0")
                .AppendLine($"map_Kd {textureFile}");
            return materialBuilder.ToString();
        }
    }
}