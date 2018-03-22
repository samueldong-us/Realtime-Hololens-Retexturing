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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Windows.Storage;

namespace Realtime_Hololens_Retexturing.Common
{
    internal static class ModelLoader
    {
        public static async Task<Model> LoadObj(DeviceResources deviceResources, string path)
        {
            var folder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            var fileContent = await FileIO.ReadLinesAsync(await folder.GetFileAsync(path));

            var positions = new List<Vector3>();
            var uvs = new List<Vector2>();
            var normals = new List<Vector3>();
            var output = new List<VertexPositionNormalUV>();
            foreach (var line in fileContent)
            {
                var parts = line.Split(' ');
                if (parts[0] == "v")
                {
                    positions.Add(new Vector3(float.Parse(parts[1]), float.Parse(parts[2]), float.Parse(parts[3])));
                }
                else if (parts[0] == "vt")
                {
                    uvs.Add(new Vector2(float.Parse(parts[1]), float.Parse(parts[2])));
                }
                else if (parts[0] == "vn")
                {
                    normals.Add(new Vector3(float.Parse(parts[1]), float.Parse(parts[2]), float.Parse(parts[3])));
                }
                else if (parts[0] == "f")
                {
                    for (int i = 1; i <= 3; i++)
                    {
                        var indices = parts[i].Split('/').Select(index => int.Parse(index) - 1).ToArray();
                        output.Add(new VertexPositionNormalUV
                        {
                            Position = positions[indices[0]],
                            UV = uvs[indices[1]],
                            Normal = normals[indices[2]]
                        });
                    }
                }
            }
            return new Model(deviceResources, output.ToArray());
        }
    }
}