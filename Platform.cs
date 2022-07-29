using System.Runtime.InteropServices;

namespace Propane
{
    internal enum Endianness
    {
        Unknown,
        Little,
        Big,
        LittleWord,
        BigWord,
    }

    internal enum Architecture
    {
        Unknown,
        x32,
        x64,
    }

    internal static class Platform
    {
        [StructLayout(LayoutKind.Explicit)]
        private struct EndianBytes32
        {
            [StructLayout(LayoutKind.Explicit)]
            public struct Bytes
            {
                [FieldOffset(0)] public byte b0;
                [FieldOffset(1)] public byte b1;
                [FieldOffset(2)] public byte b2;
                [FieldOffset(3)] public byte b3;
            }

            [FieldOffset(0)] public Bytes bytes;
            [FieldOffset(0)] public uint value;
        }

        public static Endianness Endianness
        {
            get
            {
                EndianBytes32 u32 = new()
                {
                    bytes = new EndianBytes32.Bytes
                    {
                        b0 = 0x01,
                        b1 = 0x02,
                        b2 = 0x03,
                        b3 = 0x04,
                    }
                };

                return u32.value switch
                {
                    0x04030201u => Endianness.Little,
                    0x01020304u => Endianness.Big,
                    0x02010403u => Endianness.LittleWord,
                    0x03040102u => Endianness.BigWord,
                    _ => Endianness.Unknown,
                };
            }
        }

        public static Architecture Architecture
        {
            get
            {
                unsafe
                {
                    switch (sizeof(nint))
                    {
                        case 4: return Architecture.x32;
                        case 8: return Architecture.x64;
                    }
                }

                return Architecture.Unknown;
            }
        }

        public static ulong VersionNumber
        {
            get
            {
                unsafe
                {
                    ulong result = 0;
                    byte* ptr = (byte*)&result;
                    int idx = 0;

                    ulong u64 = Version.VersionMajor;
                    for (int i = 0; i < 2; i++)
                    {
                        ptr[idx++] = (byte)u64;
                        u64 >>= 8;
                    }

                    u64 = Version.VersionMinor;
                    for (int i = 0; i < 2; i++)
                    {
                        ptr[idx++] = (byte)u64;
                        u64 >>= 8;
                    }

                    u64 = Version.VersionChangelist;
                    for (int i = 0; i < 3; i++)
                    {
                        ptr[idx++] = (byte)u64;
                        u64 >>= 8;
                    }

                    byte endianArch = 0;
                    endianArch |= (byte)((int)Endianness << 4);
                    endianArch |= (byte)Architecture;
                    ptr[idx] = endianArch;

                    return result;
                }
            }
        }
    }
}
