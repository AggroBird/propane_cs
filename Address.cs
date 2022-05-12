using System;
using System.Runtime.InteropServices;

namespace Propane
{
    using Index = UInt32;

    [StructLayout(LayoutKind.Explicit, Pack = 4)]
    public struct Address
    {
        internal enum Type : byte
        {
            Stackvar,
            Parameter,
            Global,
            Constant,
        }

        internal enum Prefix : byte
        {
            None,
            Indirection,
            AddressOf,
            SizeOf,
        }

        internal enum Modifier : byte
        {
            None,
            DirectField,
            IndirectField,
            Offset,
        }

        [StructLayout(LayoutKind.Explicit, Pack = 4)]
        internal struct Header
        {
            private const Index FlagMask = 0b11;
            private const int IndexBitCount = 26;
            private const int TypeOffset = 30;
            private const int PrefixOffset = 28;
            private const int ModifierOffset = 26;
            internal const Index IndexMax = 0xFFFFFFFF >> (32 - IndexBitCount);

            [FieldOffset(0)] public Index value;

            public Header(Index value)
            {
                this.value = value;
            }
            public Header(Type type, Prefix prefix, Modifier modifier, Index index)
            {
                value = index & IndexMax;
                value |= ((Index)type & FlagMask) << TypeOffset;
                value |= ((Index)prefix & FlagMask) << PrefixOffset;
                value |= ((Index)modifier & FlagMask) << ModifierOffset;
            }
            public Header(TypeIdx constantType)
            {
                value = (Index)constantType & IndexMax;
                value |= ((Index)Type.Constant) << TypeOffset;
            }

            public Type type
            {
                get
                {
                    return (Type)((value >> TypeOffset) & FlagMask);
                }
                set
                {
                    this.value &= ~(FlagMask << TypeOffset);
                    this.value |= ((Index)value & FlagMask) << TypeOffset;
                }
            }
            public Prefix prefix
            {
                get
                {
                    return (Prefix)((value >> PrefixOffset) & FlagMask);
                }
                set
                {
                    this.value &= ~(FlagMask << PrefixOffset);
                    this.value |= ((Index)value & FlagMask) << PrefixOffset;
                }
            }
            public Modifier modifier
            {
                get
                {
                    return (Modifier)((value >> ModifierOffset) & FlagMask);
                }
                set
                {
                    this.value &= ~(FlagMask << ModifierOffset);
                    this.value |= ((Index)value & FlagMask) << ModifierOffset;
                }
            }
            public Index index
            {
                get
                {
                    return value & IndexMax;
                }
                set
                {
                    this.value &= ~IndexMax;
                    this.value |= value & IndexMax;
                }
            }
        }

        [StructLayout(LayoutKind.Explicit, Pack = 4)]
        internal struct Payload
        {
            [FieldOffset(0)] public sbyte i8;
            [FieldOffset(0)] public byte u8;
            [FieldOffset(0)] public short i16;
            [FieldOffset(0)] public ushort u16;
            [FieldOffset(0)] public int i32;
            [FieldOffset(0)] public uint u32;
            [FieldOffset(0)] public long i64;
            [FieldOffset(0)] public ulong u64;
            [FieldOffset(0)] public float f32;
            [FieldOffset(0)] public double f64;
            [FieldOffset(0)] public nuint vptr;
            [FieldOffset(0)] public NameIdx global;
            [FieldOffset(0)] public OffsetIdx field;
            [FieldOffset(0)] public nint offset;
        }

        [FieldOffset(0)] internal Header header;
        [FieldOffset(4)] internal Payload payload;

        internal Address(Index index, Type type, Modifier modifier = Modifier.None, Prefix prefix = Prefix.None)
        {
            header = new Header(type, prefix, modifier, index);
            payload = new Payload { u64 = 0 };
        }
    }

    [StructLayout(LayoutKind.Explicit, Pack = 4)]
    internal struct AddressPayload
    {
        [FieldOffset(0)] public OffsetIdx field;
        [FieldOffset(0)] public nint offset;
    }

    public struct Constant
    {
        public Constant(sbyte i8)
        {
            type = TypeIdx.I8;
            value = new Address.Payload { i8 = i8 };
        }
        public Constant(byte u8)
        {
            type = TypeIdx.U8;
            value = new Address.Payload { u8 = u8 };
        }
        public Constant(short i16)
        {
            type = TypeIdx.I16;
            value = new Address.Payload { i16 = i16 };
        }
        public Constant(ushort u16)
        {
            type = TypeIdx.U16;
            value = new Address.Payload { u16 = u16 };
        }
        public Constant(int i32)
        {
            type = TypeIdx.I32;
            value = new Address.Payload { i32 = i32 };
        }
        public Constant(uint u32)
        {
            type = TypeIdx.U32;
            value = new Address.Payload { u32 = u32 };
        }
        public Constant(long i64)
        {
            type = TypeIdx.I64;
            value = new Address.Payload { i64 = i64 };
        }
        public Constant(ulong u64)
        {
            type = TypeIdx.U64;
            value = new Address.Payload { u64 = u64 };
        }
        public Constant(float f32)
        {
            type = TypeIdx.F32;
            value = new Address.Payload { f32 = f32 };
        }
        public Constant(double f64)
        {
            type = TypeIdx.F64;
            value = new Address.Payload { f64 = f64 };
        }
        public Constant(NameIdx global)
        {
            type = TypeIdx.Void;
            value = new Address.Payload { global = global };
        }

        public static readonly Constant NullPtr = new Constant { type = TypeIdx.VPtr, value = new Address.Payload { vptr = 0 }, };

        internal TypeIdx type;
        internal Address.Payload value;

        public static implicit operator Address(Constant constant)
        {
            return new Address
            {
                header = new Address.Header(constant.type),
                payload = constant.value,
            };
        }
    }

    public interface IPrefixable
    {
        public Address Deref();
        public Address AddrOf();
        public Address SizeOf();
    }

    public struct Prefixable : IPrefixable
    {
        internal Prefixable(Address.Type type, Index index, OffsetIdx field, bool direct)
        {
            this.type = type;
            this.index = index;
            modifier = direct ? Address.Modifier.DirectField : Address.Modifier.IndirectField;
            payload = new Address.Payload { field = field, };
        }
        internal Prefixable(Address.Type type, Index index, nint offset)
        {
            this.type = type;
            this.index = index;
            modifier = Address.Modifier.Offset;
            payload = new Address.Payload { offset = offset, };
        }

        public static implicit operator Address(Prefixable prefixable)
        {
            return new Address
            {
                header = new Address.Header(prefixable.type, Address.Prefix.None, prefixable.modifier, prefixable.index),
                payload = prefixable.payload,
            };
        }

        public Address Deref()
        {
            return new Address
            {
                header = new Address.Header(type, Address.Prefix.Indirection, modifier, index),
                payload = payload,
            };
        }
        public Address AddrOf()
        {
            return new Address
            {
                header = new Address.Header(type, Address.Prefix.AddressOf, modifier, index),
                payload = payload,
            };
        }
        public Address SizeOf()
        {
            return new Address
            {
                header = new Address.Header(type, Address.Prefix.SizeOf, modifier, index),
                payload = payload,
            };
        }

        internal Address.Type type;
        internal Index index;
        internal Address.Modifier modifier;
        internal Address.Payload payload;
    }

    public interface IModifyable
    {
        public Prefixable this[nint offset] { get; }
        public Prefixable Field(OffsetIdx field);
        public Prefixable DerefField(OffsetIdx field);
    }

    public struct Stack : IPrefixable, IModifyable
    {
        private const Address.Type Type = Address.Type.Stackvar;

        public Stack(Index index)
        {
            this.index = index;
        }

        public static implicit operator Address(Stack stackvar)
        {
            return new Address
            {
                header = new Address.Header(Address.Type.Stackvar, Address.Prefix.None, Address.Modifier.None, stackvar.index),
            };
        }

        public Address Deref()
        {
            return new Address(index, Type, prefix: Address.Prefix.Indirection);
        }
        public Address AddrOf()
        {
            return new Address(index, Type, prefix: Address.Prefix.AddressOf);
        }
        public Address SizeOf()
        {
            return new Address(index, Type, prefix: Address.Prefix.SizeOf);
        }

        public Prefixable this[nint offset]
        {
            get
            {
                return new Prefixable(Type, index, offset);
            }
        }
        public Prefixable Field(OffsetIdx field)
        {
            return new Prefixable(Type, index, field, true);
        }
        public Prefixable DerefField(OffsetIdx field)
        {
            return new Prefixable(Type, index, field, false);
        }

        internal Index index;

        public static readonly Stack RetVal = new Stack(Address.Header.IndexMax);
    }

    public struct Param : IPrefixable, IModifyable
    {
        private const Address.Type Type = Address.Type.Parameter;

        public Param(Index index)
        {
            this.index = index;
        }

        public static implicit operator Address(Param param)
        {
            return new Address
            {
                header = new Address.Header(Address.Type.Parameter, Address.Prefix.None, Address.Modifier.None, param.index),
            };
        }

        public Address Deref()
        {
            return new Address(index, Type, prefix: Address.Prefix.Indirection);
        }
        public Address AddrOf()
        {
            return new Address(index, Type, prefix: Address.Prefix.AddressOf);
        }
        public Address SizeOf()
        {
            return new Address(index, Type, prefix: Address.Prefix.SizeOf);
        }

        public Prefixable this[nint offset]
        {
            get
            {
                return new Prefixable(Type, index, offset);
            }
        }
        public Prefixable Field(OffsetIdx field)
        {
            return new Prefixable(Type, index, field, true);
        }
        public Prefixable DerefField(OffsetIdx field)
        {
            return new Prefixable(Type, index, field, false);
        }

        internal Index index;
    }

    public struct Global : IPrefixable, IModifyable
    {
        private const Address.Type Type = Address.Type.Global;

        public Global(NameIdx name)
        {
            this.name = name;
        }

        public static implicit operator Address(Global global)
        {
            return new Address
            {
                header = new Address.Header(Address.Type.Global, Address.Prefix.None, Address.Modifier.None, (Index)global.name),
            };
        }

        public Address Deref()
        {
            return new Address((Index)name, Type, prefix: Address.Prefix.Indirection);
        }
        public Address AddrOf()
        {
            return new Address((Index)name, Type, prefix: Address.Prefix.AddressOf);
        }
        public Address SizeOf()
        {
            return new Address((Index)name, Type, prefix: Address.Prefix.SizeOf);
        }

        public Prefixable this[nint offset]
        {
            get
            {
                return new Prefixable(Type, (Index)name, offset);
            }
        }
        public Prefixable Field(OffsetIdx field)
        {
            return new Prefixable(Type, (Index)name, field, true);
        }
        public Prefixable DerefField(OffsetIdx field)
        {
            return new Prefixable(Type, (Index)name, field, false);
        }

        internal NameIdx name;
    }
}
