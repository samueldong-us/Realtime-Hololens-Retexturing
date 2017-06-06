using System.Numerics;
using System.Runtime.InteropServices;

namespace Realistic_Hololens_Rendering.Common
{
    internal static class Utilities
    {
        public static Matrix4x4 ToMatrix4x4(this byte[] data)
        {
            var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                return Marshal.PtrToStructure<Matrix4x4>(handle.AddrOfPinnedObject());
            }
            finally
            {
                handle.Free();
            }
        }
    }
}