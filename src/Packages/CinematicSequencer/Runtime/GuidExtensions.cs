// The original source code is available in the following article.
// https://neue.cc/2024/11/19_cysharp_oss.html

using System.Runtime.CompilerServices;

namespace System
{
    public static class GuidExtensions
    {
        private const byte Variant10xxMask = 0xC0;
        private const byte Variant10xxValue = 0x80;
        private const ushort VersionMask = 0xF000;
        private const ushort Version7Value = 0x7000;

        public static Guid CreateVersion7() => CreateVersion7(DateTimeOffset.UtcNow);

        public static Guid CreateVersion7(DateTimeOffset timestamp)
        {
            Guid result = Guid.NewGuid();

            var unix_ts_ms = timestamp.ToUnixTimeMilliseconds();

            // GUID layout is int _a; short _b; short _c, byte _d;
            Unsafe.As<Guid, int>(ref Unsafe.AsRef(result)) = (int)(unix_ts_ms >> 16); // _a
            Unsafe.Add(ref Unsafe.As<Guid, short>(ref Unsafe.AsRef(result)), 2) = (short)(unix_ts_ms); // _b

            ref var c = ref Unsafe.Add(ref Unsafe.As<Guid, short>(ref Unsafe.AsRef(result)), 3);
            c = (short)((c & ~VersionMask) | Version7Value);

            ref var d = ref Unsafe.Add(ref Unsafe.As<Guid, byte>(ref Unsafe.AsRef(result)), 8);
            d = (byte)((d & ~Variant10xxMask) | Variant10xxValue);

            return result;
        }

        public static DateTimeOffset GetTimestamp(in Guid guid)
        {
            ref var p = ref Unsafe.As<Guid, byte>(ref Unsafe.AsRef(in guid));
            var lower = Unsafe.ReadUnaligned<uint>(ref p);
            var upper = Unsafe.ReadUnaligned<ushort>(ref Unsafe.Add(ref p, 4));
            var time = (long)upper + (((long)lower) << 16);
            return DateTimeOffset.FromUnixTimeMilliseconds(time);
        }
    }
}