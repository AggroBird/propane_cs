using System;

namespace Propane
{
    using Index = UInt32;

    internal enum Opcode : byte
    {
        Noop,

        Set,
        Conv,

        AriNot,
        AriNeg,
        AriMul,
        AriDiv,
        AriMod,
        AriAdd,
        AriSub,
        AriLsh,
        AriRsh,
        AriAnd,
        AriXor,
        AriOr,

        PAdd,
        PSub,
        PDif,

        Cmp,
        Ceq,
        Cne,
        Cgt,
        Cge,
        Clt,
        Cle,
        Cze,
        Cnz,

        Br,
        Beq,
        Bne,
        Bgt,
        Bge,
        Blt,
        Ble,
        Bze,
        Bnz,

        Sw,

        Call,
        Callv,
        Ret,
        Retv,

        Dump,
    };

    public enum TypeIdx : Index
    {
        I8,
        U8,
        I16,
        U16,
        I32,
        U32,
        I64,
        U64,
        F32,
        F64,
        VPtr,
        Void,

        Invalid = Language.InvalidIndex,
    }

    public enum MethodIdx : Index { Invalid = Language.InvalidIndex };
    public enum SignatureIdx : Index { Invalid = Language.InvalidIndex };
    public enum NameIdx : Index { Invalid = Language.InvalidIndex };
    public enum LabelIdx : Index { Invalid = Language.InvalidIndex };
    public enum OffsetIdx : Index { Invalid = Language.InvalidIndex };
    internal enum GlobalIdx : Index { Invalid = Language.InvalidIndex };
    internal enum MetaIdx : Index { Invalid = Language.InvalidIndex };

    internal struct BaseTypeInfo
    {
        public BaseTypeInfo(TypeIdx index, string name, nuint size)
        {
            this.index = index;
            this.name = name;
            this.size = size;
        }

        public readonly TypeIdx index;
        public readonly string name;
        public readonly nuint size;
    }

    internal static class Language
    {
        public const Index InvalidIndex = 0xFFFFFFFF;

        public const int MaxParameterCount = 256;
        public const int MaxInitializerCount = 65536;

        public const string IntermediateHeader = "PINT";
        public const string Footer = "END";

        public static readonly BaseTypeInfo[] BaseTypes =
        {
            new BaseTypeInfo(TypeIdx.I8, "byte", 1),
            new BaseTypeInfo(TypeIdx.U8, "ubyte", 1),
            new BaseTypeInfo(TypeIdx.I16, "short", 2),
            new BaseTypeInfo(TypeIdx.U16, "ushort", 2),
            new BaseTypeInfo(TypeIdx.I32, "int", 4),
            new BaseTypeInfo(TypeIdx.U32, "uint", 4),
            new BaseTypeInfo(TypeIdx.I64, "long", 8),
            new BaseTypeInfo(TypeIdx.U64, "ulong", 8),
            new BaseTypeInfo(TypeIdx.F32, "float", 4),
            new BaseTypeInfo(TypeIdx.F64, "double", 8),
            new BaseTypeInfo(TypeIdx.VPtr, "void*", (nuint)(Platform.architecture == Architecture.x32 ? 4 : 8)),
            new BaseTypeInfo(TypeIdx.Void, "void", 0),
        };

        public static readonly string[] AliasTypes =
        {
            "offset",
            "size",
        };
    }
}
