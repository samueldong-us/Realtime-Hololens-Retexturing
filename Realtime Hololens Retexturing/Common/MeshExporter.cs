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
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.WIC;
using System;
using System.IO;
using System.Text;
using Windows.Storage;

namespace Realtime_Hololens_Retexturing.Common
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