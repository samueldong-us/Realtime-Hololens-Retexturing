using System.Numerics;
using System.Runtime.InteropServices;

namespace Realtime_Hololens_Retexturing.Common
{
    internal static class Utilities
    {
        public static T To<T>(this byte[] data)
        {
            var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                return Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
            }
            finally
            {
                handle.Free();
            }
        }
    }
}