using Realistic_Hololens_Rendering.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Windows.Storage;

namespace Realistic_Hololens_Rendering.Common
{
    static class MeshLoader
    {
        public static async Task<Mesh> LoadObj(DeviceResources deviceResources, string path)
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
            return new Mesh(deviceResources, output.ToArray());
        }
    }
}
