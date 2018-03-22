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
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using System.Numerics;
using SharpDX.Direct3D11;

namespace Realtime_Hololens_Retexturing.Common
{
    internal static class DirectXHelper
    {
        /// <summary>
        /// Function that reads from a binary file asynchronously.
        /// </summary>
        internal static async Task<byte[]> ReadDataAsync(StorageFile file)
        {
            using (var stream = await file.OpenStreamForReadAsync())
            {
                byte[] buffer = new byte[stream.Length];
                await stream.ReadAsync(buffer, 0, buffer.Length);
                return buffer;
            }
        }

        private const float DipsPerInch = 96.0f;

        /// <summary>
        /// Converts a length in device-independent pixels (DIPs) to a length in physical pixels.
        /// </summary>
        internal static double ConvertDipsToPixels(double dips, double dpi)
        {
            return Math.Floor(dips * dpi / DipsPerInch + 0.5f); // Round to nearest integer.
        }

        internal static async Task<T> LoadShader<T>(Device device, StorageFolder folder, string filename)
        {
            var shaderBytecode = await ReadDataAsync(await folder.GetFileAsync(filename));
            return (T)Activator.CreateInstance(typeof(T), device, shaderBytecode, (ClassLinkage)null);
        }

#if DEBUG
        /// <summary>
        /// Check for SDK Layer support.
        /// </summary>
        internal static bool SdkLayersAvailable()
        {
            try
            {
                using (var device = new SharpDX.Direct3D11.Device(SharpDX.Direct3D.DriverType.Null, SharpDX.Direct3D11.DeviceCreationFlags.Debug))
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
#endif
    }
}
