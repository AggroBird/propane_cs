using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Propane
{
    using Index = UInt32;

    public struct Field
    {
        public Field(NameIdx name, TypeIdx type)
        {
            this.name = name;
            this.type = type;
            offset = 0;
        }
        internal Field(NameIdx name, TypeIdx type, nuint offset)
        {
            this.name = name;
            this.type = type;
            this.offset = offset;
        }

        public NameIdx name;
        public TypeIdx type;
        internal nuint offset;
    }

    internal static class BinaryWriterHelper
    {
        public static void WriteConstant(this BinaryWriter writer, Constant constant)
        {
            switch (constant.type)
            {
                case TypeIdx.I8: writer.Write(constant.value.i8); break;
                case TypeIdx.U8: writer.Write(constant.value.u8); break;
                case TypeIdx.I16: writer.Write(constant.value.i16); break;
                case TypeIdx.U16: writer.Write(constant.value.u16); break;
                case TypeIdx.I32: writer.Write(constant.value.i32); break;
                case TypeIdx.U32: writer.Write(constant.value.u32); break;
                case TypeIdx.I64: writer.Write(constant.value.i64); break;
                case TypeIdx.U64: writer.Write(constant.value.u64); break;
                case TypeIdx.F32: writer.Write(constant.value.f32); break;
                case TypeIdx.F64: writer.Write(constant.value.f64); break;
                case TypeIdx.VPtr: writer.Write(constant.value.vptr); break;
                default: throw new Exception();
            }
        }
        public static void WriteConstant(this BinaryWriter writer, Address constant)
        {
            switch ((TypeIdx)constant.header.Index)
            {
                case TypeIdx.I8: writer.Write(constant.payload.i8); break;
                case TypeIdx.U8: writer.Write(constant.payload.u8); break;
                case TypeIdx.I16: writer.Write(constant.payload.i16); break;
                case TypeIdx.U16: writer.Write(constant.payload.u16); break;
                case TypeIdx.I32: writer.Write(constant.payload.i32); break;
                case TypeIdx.U32: writer.Write(constant.payload.u32); break;
                case TypeIdx.I64: writer.Write(constant.payload.i64); break;
                case TypeIdx.U64: writer.Write(constant.payload.u64); break;
                case TypeIdx.F32: writer.Write(constant.payload.f32); break;
                case TypeIdx.F64: writer.Write(constant.payload.f64); break;
                case TypeIdx.VPtr: writer.Write(constant.payload.vptr); break;
                default: throw new Exception();
            }
        }
    }

    internal struct BinaryKey
    {
        public BinaryKey(ref BinaryWriter keyBuf, TypeIdx type, TypeIdx param)
        {
            keyBuf.Clear();

            AppendKey(keyBuf, (ulong)type);
            AppendKey(keyBuf, (ulong)param);

            value = keyBuf.ToArray();
        }
        public BinaryKey(ref BinaryWriter keyBuf, TypeIdx type, IEnumerable<TypeIdx>? param)
        {
            keyBuf.Clear();

            AppendKey(keyBuf, (ulong)type);
            if (param != null)
            {
                foreach (var p in param)
                {
                    AppendKey(keyBuf, (ulong)p);
                }
            }

            value = keyBuf.ToArray();
        }
        public BinaryKey(ref BinaryWriter keyBuf, TypeIdx type, NameIdx param)
        {
            keyBuf.Clear();

            AppendKey(keyBuf, (ulong)type);
            AppendKey(keyBuf, (ulong)param);

            value = keyBuf.ToArray();
        }
        public BinaryKey(ref BinaryWriter keyBuf, TypeIdx type, IEnumerable<NameIdx>? param)
        {
            keyBuf.Clear();

            AppendKey(keyBuf, (ulong)type);
            if (param != null)
            {
                foreach (var p in param)
                {
                    AppendKey(keyBuf, (ulong)p);
                }
            }

            value = keyBuf.ToArray();
        }

        private static void AppendKey(BinaryWriter keyBuf, ulong val)
        {
            if (val <= (0xFF >> 2))
            {
                keyBuf.Write((byte)(val << 2));
            }
            else if (val <= (0xFFFF >> 2))
            {
                keyBuf.Write((ushort)(val << 2) | 1);
            }
            else if (val <= (0xFFFFFFFF >> 2))
            {
                keyBuf.Write((uint)(val << 2) | 2);
            }
            else
            {
                keyBuf.Write((byte)3);
                keyBuf.Write(val);
            }
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)2166136261;
                for (int i = 0; i < value.Length; i++)
                {
                    hash = (hash * 16777619) ^ value[i].GetHashCode();
                }
                return hash;
            }
        }
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is BinaryKey other && Equals(other);
        }
        public bool Equals(BinaryKey other)
        {
            if (value.Length == other.value.Length)
            {
                for (int i = 0; i < value.Length; i++)
                {
                    if (value[i] != other.value[i])
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        public static bool operator ==(BinaryKey lhs, BinaryKey rhs) => lhs.Equals(rhs);
        public static bool operator !=(BinaryKey lhs, BinaryKey rhs) => !lhs.Equals(rhs);

        private readonly byte[] value;
    }

    internal struct LookupIdx
    {
        public LookupIdx(TypeIdx type)
        {
            lookup = LookupType.Type;
            value = new Value { type = type };
        }
        public LookupIdx(MethodIdx method)
        {
            lookup = LookupType.Method;
            value = new Value { method = method };
        }

        public enum LookupType
        {
            Type,
            Method,
            Global,
            Constant,
            Identifier,
        }

        [StructLayout(LayoutKind.Explicit, Pack = 4)]
        public struct Value
        {
            [FieldOffset(0)] public TypeIdx type;
            [FieldOffset(0)] public MethodIdx method;
            [FieldOffset(0)] public Index index;
        }

        public LookupType lookup;
        public Value value;


        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is Address other && Equals(other);
        }
        public bool Equals(LookupIdx other)
        {
            return lookup == other.lookup && value.index == other.value.index;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)2166136261;
                hash = (hash * 16777619) ^ value.index.GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(LookupIdx lhs, LookupIdx rhs) => lhs.Equals(rhs);
        public static bool operator !=(LookupIdx lhs, LookupIdx rhs) => !lhs.Equals(rhs);
    }

    public class Generator
    {
        private enum TypeFlags
        {
            None = 0,
            IsUnion = 1 << 0,

            IsPointer = 1 << 8,
            IsArray = 1 << 9,
            IsSignature = 1 << 10,

            IsDefined = 1 << 24,
            IsResolved = 1 << 26,

            IsGenerated = IsPointer | IsArray | IsSignature,
        }

        public Generator(string? fileName = null)
        {
            // Initialize base types
            for (int i = 0; i < Language.BaseTypes.Length; i++)
            {
                NameIdx name = database.Emplace(Language.BaseTypes[i].name, new LookupIdx(Language.BaseTypes[i].index));
                types.Add(new Type(name, Language.BaseTypes[i]));
            }

            // Initialize aliases
            TypeIdx sizeTypes = Platform.Architecture == Architecture.x32 ? TypeIdx.I32 : TypeIdx.I64;
            for (int i = 0; i < Language.AliasTypes.Length; i++)
            {
                database.Emplace(Language.AliasTypes[i], new LookupIdx(sizeTypes + (Index)i));
            }

            // Generate file meta
            if (fileName != null)
            {
                this.fileName = fileName;
                meta = 0;
            }
            else
            {
                this.fileName = string.Empty;
            }
        }

        private class Type
        {
            public Type(NameIdx name, TypeIdx index)
            {
                this.name = name;
                this.index = index;
            }
            public Type(NameIdx name, BaseTypeInfo baseType)
            {
                this.name = name;
                index = baseType.index;

                if (index == TypeIdx.Void)
                {
                    pointerType = TypeIdx.VPtr;
                }
                else if (index == TypeIdx.VPtr)
                {
                    MakePointer(TypeIdx.Void);
                }

                totalSize = baseType.size;

                flags |= TypeFlags.IsDefined | TypeFlags.IsResolved;
            }

            public readonly NameIdx name;
            public readonly TypeIdx index;
            public TypeFlags flags;
            public bool IsDefined => (flags & TypeFlags.IsDefined) != TypeFlags.None;

            [StructLayout(LayoutKind.Explicit, Pack = 4)]
            public struct GeneratedType
            {
                [StructLayout(LayoutKind.Explicit, Pack = 4)]
                public struct PointerData
                {
                    [FieldOffset(0)] public TypeIdx underlyingType;
                }

                [StructLayout(LayoutKind.Explicit, Pack = 4)]
                public struct ArrayData
                {
                    [FieldOffset(0)] public TypeIdx underlyingType;
                    [FieldOffset(4)] public nuint arraySize;
                }

                [StructLayout(LayoutKind.Explicit, Pack = 4)]
                public struct SignatureData
                {
                    [FieldOffset(0)] public SignatureIdx signature;
                }

                [FieldOffset(0)] public PointerData pointer;
                [FieldOffset(0)] public ArrayData array;
                [FieldOffset(0)] public SignatureData signature;
            }

            public GeneratedType generated;

            public Field[] fields = Array.Empty<Field>();
            public nuint totalSize = 0;

            public TypeIdx pointerType = TypeIdx.Invalid;
            public Dictionary<nuint, TypeIdx>? arrayTypes = null;

            public MetaIdx meta = MetaIdx.Invalid;
            public Index lineNumber = 0;

            public void MakePointer(TypeIdx underlyingType)
            {
                generated = new GeneratedType
                {
                    pointer = new GeneratedType.PointerData
                    {
                        underlyingType = underlyingType
                    }
                };
                flags |= TypeFlags.IsPointer;
            }
            public void MakeArray(TypeIdx underlyingType, nuint arraySize)
            {
                generated = new GeneratedType
                {
                    array = new GeneratedType.ArrayData
                    {
                        underlyingType = underlyingType,
                        arraySize = arraySize
                    }
                };
                flags |= TypeFlags.IsArray;
            }
            public void MakeSignature(SignatureIdx signature)
            {
                generated = new GeneratedType
                {
                    signature = new GeneratedType.SignatureData
                    {
                        signature = signature
                    }
                };
                flags |= TypeFlags.IsSignature;
            }

            public TypeWriter? writer = null;
        }

        public sealed class TypeWriter
        {
            internal TypeWriter(Generator gen, NameIdx name, TypeIdx index, bool isUnion)
            {
                this.gen = gen;
                this.name = name;
                this.index = index;
                this.isUnion = isUnion;

                meta = gen.meta;
                lineNumber = gen.lineNumber;
            }

            private readonly Generator gen;

            public readonly NameIdx name;
            public readonly TypeIdx index;
            public readonly bool isUnion;

            public void DeclareField(TypeIdx type, NameIdx name)
            {
                if (!gen.database.ContainsKey(name))
                {
                    throw new Exception();
                }

                var fieldType = gen.GetType(type);
                foreach (var field in fieldList)
                {
                    if (field.name == name)
                    {
                        throw new Exception();
                    }
                }

                fieldList.Add(new Field(name, fieldType.index));
            }
            public NameIdx DeclareField(TypeIdx type, string name)
            {
                NameIdx id = gen.MakeIdentifier(name);
                DeclareField(type, id);
                return id;
            }

            public IReadOnlyList<Field> Fields => fieldList;

            private readonly List<Field> fieldList = new();

            private readonly MetaIdx meta;
            private readonly Index lineNumber;

            public void Export()
            {
                var dst = gen.GetType(index);
                if (dst.IsDefined)
                {
                    throw new Exception();
                }

                dst.flags |= TypeFlags.IsDefined;
                if (isUnion) dst.flags |= TypeFlags.IsUnion;

                dst.fields = fieldList.ToArray();
                dst.meta = meta;
                dst.lineNumber = lineNumber;

                dst.writer = null;
            }
        }

        private class Method
        {
            public Method(NameIdx name, MethodIdx index)
            {
                this.name = name;
                this.index = index;
            }

            public readonly NameIdx name;
            public readonly MethodIdx index;
            public TypeFlags flags;
            public bool IsDefined => (flags & TypeFlags.IsDefined) != TypeFlags.None;

            public SignatureIdx signature = SignatureIdx.Invalid;

            public byte[] bytecode = Array.Empty<byte>();
            public nuint[] labels = Array.Empty<nuint>();
            public TypeIdx[] stackvars = Array.Empty<TypeIdx>();

            public MethodIdx[] calls = Array.Empty<MethodIdx>();
            public GlobalIdx[] globals = Array.Empty<GlobalIdx>();
            public OffsetIdx[] offsets = Array.Empty<OffsetIdx>();

            public MetaIdx meta = MetaIdx.Invalid;
            public Index lineNumber = 0;

            public MethodWriter? writer = null;
        }

        public sealed class MethodWriter
        {
            internal MethodWriter(Generator gen, NameIdx name, MethodIdx index, Signature signature)
            {
                this.gen = gen;
                this.name = name;
                this.index = index;
                signatureRef = signature;
                parameterCount = signature.parameters.Length;

                meta = gen.meta;
                lineNumber = gen.lineNumber;
            }

            private readonly Generator gen;

            public readonly NameIdx name;
            public readonly MethodIdx index;

            private readonly Signature signatureRef;
            private readonly int parameterCount;

            public void Push(IEnumerable<TypeIdx> types)
            {
                foreach (var type in types)
                {
                    stackvars.Add(type);
                }
            }
            public void Push(TypeIdx type)
            {
                stackvars.Add(type);
            }

            public IReadOnlyList<TypeIdx> Stack => stackvars;

            private readonly List<TypeIdx> stackvars = new();
            private readonly BinaryWriter bytecode = new();

            private readonly Dictionary<MethodIdx, int> callLookup = new();
            private readonly List<MethodIdx> calls = new();

            private readonly Dictionary<NameIdx, int> globalLookup = new();
            private readonly List<GlobalIdx> globals = new();

            private readonly Dictionary<OffsetIdx, int> offsetLookup = new();
            private readonly List<OffsetIdx> offsets = new();

            private readonly Dictionary<LabelIdx, int> labelLocations = new();
            private readonly Dictionary<LabelIdx, List<int>> unresolvedBranches = new();
            private readonly Database<Index, LabelIdx> namedLabels = new();
            private readonly List<Index> labelDeclarations = new();

            private readonly MetaIdx meta;
            private readonly Index lineNumber;

            private int lastReturn = 0;


            public LabelIdx DeclareLabel(string? name = null)
            {
                if (name == null)
                {
                    LabelIdx label = (LabelIdx)labelDeclarations.Count;
                    labelDeclarations.Add(Language.InvalidIndex);
                    return label;
                }
                else
                {
                    if (!ValidateIdentifier(name))
                    {
                        throw new Exception();
                    }

                    if (!namedLabels.TryGetValue(name, out var namedLabel))
                    {
                        LabelIdx label = (LabelIdx)labelDeclarations.Count;
                        labelDeclarations.Add(namedLabels.Emplace(name, label));
                        return label;
                    }
                    return namedLabel.value;
                }
            }
            public void WriteLabel(LabelIdx label)
            {
                if ((int)label >= labelDeclarations.Count)
                {
                    throw new Exception();
                }

                if (labelLocations.ContainsKey(label))
                {
                    throw new Exception();
                }

                labelLocations.Add(label, bytecode.Count);
            }

            private bool ValidateAddress(Address addr)
            {
                switch (addr.header.Type)
                {
                    case Address.Type.Stackvar:
                    {
                        if (addr.header.Index != Address.Header.IndexMax)
                        {
                            if (addr.header.Index >= stackvars.Count)
                            {
                                throw new Exception();
                            }
                        }
                    }
                    break;

                    case Address.Type.Parameter:
                    {
                        if (addr.header.Index >= parameterCount)
                        {
                            throw new Exception();
                        }
                    }
                    break;

                    case Address.Type.Constant:
                    {
                        throw new Exception();
                    }
                }

                return true;
            }
            private bool ValidateOperand(Address addr)
            {
                if (addr.header.Type == Address.Type.Constant)
                {
                    if (addr.header.Prefix != Address.Prefix.None || addr.header.Modifier != Address.Modifier.None)
                    {
                        throw new Exception();
                    }

                    return true;
                }

                return ValidateAddress(addr);
            }
            private bool ValidateLabel(LabelIdx label)
            {
                if ((int)label >= labelDeclarations.Count)
                {
                    throw new Exception();
                }

                return true;
            }

            private void WriteSubcodeZero()
            {
                bytecode.Write((byte)0);
            }

            private void WriteAddress(Address addr)
            {
                Address.Header header = addr.header;
                AddressPayload data = default;

                switch (header.Type)
                {
                    case Address.Type.Global:
                    {
                        NameIdx globalName = (NameIdx)header.Index;
                        if (!globalLookup.TryGetValue(globalName, out var idx))
                        {
                            idx = globals.Count;
                            globalLookup.Add(globalName, idx);
                            globals.Add((GlobalIdx)globalName);
                            header.Index = (Index)idx;
                        }
                        header.Index = (Index)idx;
                    }
                    break;
                }

                switch (header.Modifier)
                {
                    case Address.Modifier.None: break;

                    case Address.Modifier.DirectField:
                    case Address.Modifier.IndirectField:
                    {
                        OffsetIdx field = addr.payload.field;
                        if (!offsetLookup.TryGetValue(field, out int idx))
                        {
                            idx = offsets.Count;
                            offsetLookup.Add(field, idx);
                            offsets.Add(field);
                        }
                        data.field = (OffsetIdx)idx;
                    }
                    break;

                    case Address.Modifier.Offset:
                    {
                        data.offset = addr.payload.offset;
                    }
                    break;
                }

                bytecode.Write(header);
                bytecode.Write(data);
            }
            private void WriteOperand(Address addr)
            {
                if (addr.header.Type == Address.Type.Constant)
                {
                    bytecode.Write(addr.header);
                    bytecode.WriteConstant(addr);
                    return;
                }

                WriteAddress(addr);
            }

            private void WriteLabelAddr(LabelIdx label)
            {
                if (!unresolvedBranches.TryGetValue(label, out List<int>? locations))
                {
                    locations = new List<int>();
                    unresolvedBranches.Add(label, locations);
                }
                locations.Add(bytecode.Count);

                bytecode.Write<nuint>(0);
            }

            private void WriteExpression(Opcode op, Address lhs)
            {
                if (ValidateAddress(lhs))
                {
                    bytecode.Write(op);
                    WriteAddress(lhs);
                }
            }
            private void WriteExpression(Opcode op, Address lhs, Address rhs)
            {
                if (ValidateAddress(lhs) && ValidateOperand(rhs))
                {
                    bytecode.Write(op);
                    WriteAddress(lhs);
                    WriteOperand(rhs);
                }
            }

            private void WriteSubExpression(Opcode op, Address lhs)
            {
                if (ValidateAddress(lhs))
                {
                    bytecode.Write(op);
                    WriteSubcodeZero();
                    WriteAddress(lhs);
                }
            }
            private void WriteSubExpression(Opcode op, Address lhs, Address rhs)
            {
                if (ValidateAddress(lhs) && ValidateOperand(rhs))
                {
                    bytecode.Write(op);
                    WriteSubcodeZero();
                    WriteAddress(lhs);
                    WriteOperand(rhs);
                }
            }

            private void WriteBranch(Opcode op, LabelIdx label)
            {
                if (ValidateLabel(label))
                {
                    bytecode.Write(op);
                    WriteLabelAddr(label);
                }
            }
            private void WriteBranch(Opcode op, LabelIdx label, Address lhs)
            {
                if (ValidateLabel(label))
                {
                    if (ValidateAddress(lhs))
                    {
                        bytecode.Write(op);
                        WriteLabelAddr(label);
                        WriteSubcodeZero();
                        WriteAddress(lhs);
                    }
                }
            }
            private void WriteBranch(Opcode op, LabelIdx label, Address lhs, Address rhs)
            {
                if (ValidateLabel(label))
                {
                    if (ValidateAddress(lhs) && ValidateOperand(rhs))
                    {
                        bytecode.Write(op);
                        WriteLabelAddr(label);
                        WriteSubcodeZero();
                        WriteAddress(lhs);
                        WriteOperand(rhs);
                    }
                }
            }

            public void WriteNoop() => bytecode.Write(Opcode.Noop);

            public void WriteSet(Address lhs, Address rhs) => WriteSubExpression(Opcode.Set, lhs, rhs);
            public void WriteConv(Address lhs, Address rhs) => WriteSubExpression(Opcode.Conv, lhs, rhs);

            public void WriteNot(Address lhs) => WriteSubExpression(Opcode.AriNot, lhs);
            public void WriteNeg(Address lhs) => WriteSubExpression(Opcode.AriNeg, lhs);
            public void WriteMul(Address lhs, Address rhs) => WriteSubExpression(Opcode.AriMul, lhs, rhs);
            public void WriteDiv(Address lhs, Address rhs) => WriteSubExpression(Opcode.AriDiv, lhs, rhs);
            public void WriteMod(Address lhs, Address rhs) => WriteSubExpression(Opcode.AriMod, lhs, rhs);
            public void WriteAdd(Address lhs, Address rhs) => WriteSubExpression(Opcode.AriAdd, lhs, rhs);
            public void WriteSub(Address lhs, Address rhs) => WriteSubExpression(Opcode.AriSub, lhs, rhs);
            public void WriteLsh(Address lhs, Address rhs) => WriteSubExpression(Opcode.AriLsh, lhs, rhs);
            public void WriteRsh(Address lhs, Address rhs) => WriteSubExpression(Opcode.AriRsh, lhs, rhs);
            public void WriteAnd(Address lhs, Address rhs) => WriteSubExpression(Opcode.AriAnd, lhs, rhs);
            public void WriteXor(Address lhs, Address rhs) => WriteSubExpression(Opcode.AriXor, lhs, rhs);
            public void WriteOr(Address lhs, Address rhs) => WriteSubExpression(Opcode.AriOr, lhs, rhs);

            public void WritePAdd(Address lhs, Address rhs) => WriteSubExpression(Opcode.PAdd, lhs, rhs);
            public void WritePSub(Address lhs, Address rhs) => WriteSubExpression(Opcode.PSub, lhs, rhs);
            public void WritePDif(Address lhs, Address rhs) => WriteExpression(Opcode.PDif, lhs, rhs);

            public void WriteCmp(Address lhs, Address rhs) => WriteSubExpression(Opcode.Cmp, lhs, rhs);
            public void WriteCeq(Address lhs, Address rhs) => WriteSubExpression(Opcode.Ceq, lhs, rhs);
            public void WriteCne(Address lhs, Address rhs) => WriteSubExpression(Opcode.Cne, lhs, rhs);
            public void WriteCgt(Address lhs, Address rhs) => WriteSubExpression(Opcode.Cgt, lhs, rhs);
            public void WriteCge(Address lhs, Address rhs) => WriteSubExpression(Opcode.Cge, lhs, rhs);
            public void WriteClt(Address lhs, Address rhs) => WriteSubExpression(Opcode.Clt, lhs, rhs);
            public void WriteCle(Address lhs, Address rhs) => WriteSubExpression(Opcode.Cle, lhs, rhs);
            public void WriteCze(Address lhs) => WriteSubExpression(Opcode.Cze, lhs);
            public void WriteCnz(Address lhs) => WriteSubExpression(Opcode.Cnz, lhs);

            public void WriteBr(LabelIdx label) => WriteBranch(Opcode.Br, label);
            public void WriteBeq(LabelIdx label, Address lhs, Address rhs) => WriteBranch(Opcode.Beq, label, lhs, rhs);
            public void WriteBne(LabelIdx label, Address lhs, Address rhs) => WriteBranch(Opcode.Bne, label, lhs, rhs);
            public void WriteBgt(LabelIdx label, Address lhs, Address rhs) => WriteBranch(Opcode.Bgt, label, lhs, rhs);
            public void WriteBge(LabelIdx label, Address lhs, Address rhs) => WriteBranch(Opcode.Bge, label, lhs, rhs);
            public void WriteBlt(LabelIdx label, Address lhs, Address rhs) => WriteBranch(Opcode.Blt, label, lhs, rhs);
            public void WriteBle(LabelIdx label, Address lhs, Address rhs) => WriteBranch(Opcode.Ble, label, lhs, rhs);
            public void WriteBze(LabelIdx label, Address lhs) => WriteBranch(Opcode.Bze, label, lhs);
            public void WriteBnz(LabelIdx label, Address lhs) => WriteBranch(Opcode.Bnz, label, lhs);

            public void WriteSw(Address address, IEnumerable<LabelIdx> labels)
            {
                if (ValidateAddress(address))
                {
                    Index labelCount = 0;
                    foreach (var label in labels)
                    {
                        ValidateLabel(label);
                        labelCount++;
                    }

                    if (labelCount == 0)
                    {
                        throw new Exception();
                    }

                    bytecode.Write(Opcode.Sw);
                    WriteAddress(address);

                    bytecode.Write(labelCount);
                    foreach (var label in labels)
                    {
                        WriteLabelAddr(label);
                    }
                }
            }

            public void WriteCall(MethodIdx method, IEnumerable<Address>? args = null)
            {
                var callMethod = gen.GetMethod(method);

                gen.addressBuf.Clear();
                if (args != null)
                {
                    foreach (var arg in args)
                    {
                        if (ValidateOperand(arg))
                        {
                            gen.addressBuf.Add(arg);
                        }
                    }
                }

                if (gen.addressBuf.Count > Language.MaxParameterCount)
                {
                    throw new Exception();
                }

                bytecode.Write(Opcode.Call);
                if (!callLookup.TryGetValue(callMethod.index, out int idx))
                {
                    idx = calls.Count;
                    callLookup.Add(callMethod.index, idx);
                    calls.Add(callMethod.index);
                }
                bytecode.Write((Index)idx);

                bytecode.Write((byte)gen.addressBuf.Count);
                foreach (var arg in gen.addressBuf)
                {
                    WriteSubcodeZero();
                    WriteOperand(arg);
                }
            }
            public void WriteCallv(Address address, IEnumerable<Address>? args = null)
            {
                if (ValidateAddress(address))
                {
                    gen.addressBuf.Clear();
                    if (args != null)
                    {
                        foreach (var arg in args)
                        {
                            if (ValidateOperand(arg))
                            {
                                gen.addressBuf.Add(arg);
                            }
                        }
                    }

                    if (gen.addressBuf.Count > Language.MaxParameterCount)
                    {
                        throw new Exception();
                    }

                    bytecode.Write(Opcode.Callv);
                    WriteAddress(address);

                    bytecode.Write((byte)gen.addressBuf.Count);
                    foreach (var arg in gen.addressBuf)
                    {
                        WriteSubcodeZero();
                        WriteOperand(arg);
                    }
                }
            }
            public void WriteRet()
            {
                if (signatureRef.HasReturnValue)
                {
                    throw new Exception();
                }

                bytecode.Write(Opcode.Ret);

                lastReturn = bytecode.Count;
            }
            public void WriteRetv(Address address)
            {
                if (!signatureRef.HasReturnValue)
                {
                    throw new Exception();
                }

                if (ValidateOperand(address))
                {
                    bytecode.Write(Opcode.Retv);
                    WriteSubcodeZero();
                    WriteOperand(address);
                }

                lastReturn = bytecode.Count;
            }

            public void WriteDump(Address address)
            {
                if (ValidateOperand(address))
                {
                    bytecode.Write(Opcode.Dump);
                    WriteOperand(address);
                }
            }


            public void Export()
            {
                var dst = gen.GetMethod(index);
                if (dst.IsDefined)
                {
                    throw new Exception();
                }

                if (signatureRef.HasReturnValue)
                {
                    if (lastReturn != bytecode.Count || bytecode.Count == 0)
                    {
                        throw new Exception();
                    }
                }

                List<nuint> exportLabels = new();
                SortedDictionary<int, List<LabelIdx>> writeLabels = new();
                foreach (var branch in unresolvedBranches)
                {
                    if (!labelLocations.TryGetValue(branch.Key, out int location))
                    {
                        throw new Exception();
                    }

                    if (!writeLabels.TryGetValue(location, out List<LabelIdx>? labels))
                    {
                        labels = new List<LabelIdx>();
                        writeLabels.Add(location, labels);
                    }
                    labels.Add(branch.Key);
                }

                foreach (var label in writeLabels)
                {
                    foreach (var branchIndex in label.Value)
                    {
                        foreach (var offset in unresolvedBranches[branchIndex])
                        {
                            unsafe
                            {
                                fixed (byte* ptr = &bytecode.Data[offset])
                                {
                                    *(nuint*)ptr = (nuint)label.Key;
                                }
                            }
                        }
                    }
                    if (label.Key >= bytecode.Count)
                    {
                        if (signatureRef.HasReturnValue)
                        {
                            throw new Exception();
                        }
                        WriteRet();
                    }
                    exportLabels.Add((nuint)label.Key);
                }

                dst.flags |= TypeFlags.IsDefined;
                dst.signature = signatureRef.index;
                dst.bytecode = bytecode.ToArray();
                dst.labels = exportLabels.ToArray();
                dst.stackvars = stackvars.ToArray();
                dst.calls = calls.ToArray();
                dst.globals = globals.ToArray();
                dst.offsets = offsets.ToArray();
                dst.meta = meta;
                dst.lineNumber = lineNumber;

                dst.writer = null;
            }
        }

        private class Offset
        {
            public Offset(OffsetIdx index, TypeIdx objectType, NameIdx[] fieldNames)
            {
                this.index = index;
                this.objectType = objectType;
                this.fieldNames = fieldNames;
            }

            public readonly OffsetIdx index;
            public TypeIdx objectType;
            public NameIdx[] fieldNames;
        }

        internal class Signature
        {
            public Signature(SignatureIdx index, TypeIdx returnType, TypeIdx[] parameters)
            {
                this.index = index;
                this.returnType = returnType;
                this.parameters = parameters;
            }

            public readonly SignatureIdx index;
            public TypeIdx returnType;
            public TypeIdx[] parameters;
            public TypeIdx signatureType = TypeIdx.Invalid;

            public bool HasReturnValue => returnType != TypeIdx.Void;
        }


        private static bool ValidateIdentifier(char c, bool first)
        {
            if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_' || c == '$')
            {
                return true;
            }
            if (!first)
            {
                return (c >= '0' && c <= '9');
            }
            return false;
        }
        private static bool ValidateIdentifier(string str)
        {
            if (!string.IsNullOrEmpty(str) && ValidateIdentifier(str[0], true))
            {
                for (int i = 1; i < str.Length; i++)
                {
                    if (!ValidateIdentifier(str[i], false))
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }
        public NameIdx MakeIdentifier(string name)
        {
            if (!ValidateIdentifier(name))
            {
                throw new Exception();
            }

            return EmplaceIdentifier(name);
        }

        // Reusable temporary buffers
        private BinaryWriter keyBuf = new();
        private readonly List<Constant> constantBuf = new();
        private readonly List<TypeIdx> typeBuf = new();
        private readonly List<NameIdx> nameBuf = new();
        private readonly List<Address> addressBuf = new();
        private readonly Constant[] constantArr = new Constant[1];
        private readonly NameIdx[] nameArr = new NameIdx[1];

        public SignatureIdx MakeSignature(TypeIdx returnType, IEnumerable<TypeIdx>? parameterTypes)
        {
            var retType = GetType(returnType);
            typeBuf.Clear();
            if (parameterTypes != null)
            {
                foreach (var param in parameterTypes)
                {
                    typeBuf.Add(GetType(param).index);
                }
            }

            // Validate param count
            if (typeBuf.Count > Language.MaxParameterCount)
            {
                throw new Exception();
            }

            // Ensure unique
            BinaryKey key = new(ref keyBuf, retType.index, parameterTypes);
            if (signatureLookup.TryGetValue(key, out SignatureIdx signature))
            {
                return signature;
            }
            // Write new
            signature = (SignatureIdx)signatures.Count;
            Signature sig = new(signature, returnType, typeBuf.ToArray());
            signatureLookup.Add(key, signature);
            signatures.Add(sig);
            return signature;
        }

        public OffsetIdx MakeOffset(TypeIdx type, IEnumerable<NameIdx> fields)
        {
            // Validate fields
            var rootType = GetType(type);
            nameBuf.Clear();
            foreach (var field in fields)
            {
                if (!database.ContainsKey(field))
                {
                    throw new Exception();
                }
                nameBuf.Add(field);
            }

            // Ensure unique
            BinaryKey key = new(ref keyBuf, rootType.index, fields);
            if (offsetLookup.TryGetValue(key, out OffsetIdx offset))
            {
                return offset;
            }

            // Write new
            offset = (OffsetIdx)offsets.Count;
            Offset off = new(offset, rootType.index, nameBuf.ToArray());
            offsetLookup.Add(key, offset);
            offsets.Add(off);
            return offset;
        }
        public OffsetIdx MakeOffset(TypeIdx type, NameIdx field)
        {
            nameArr[0] = field;
            return MakeOffset(type, nameArr);
        }
        public OffsetIdx AppendOffset(OffsetIdx offset, IEnumerable<NameIdx> fields)
        {
            // Validate fields
            Offset off = GetOffset(offset);
            nameBuf.Clear();
            nameBuf.AddRange(off.fieldNames);
            foreach (var field in fields)
            {
                if (!database.ContainsKey(field))
                {
                    throw new Exception();
                }
                nameBuf.Add(field);
            }

            // Append
            NameIdx[] appended = nameBuf.ToArray();
            BinaryKey key = new(ref keyBuf, off.objectType, appended);

            // Ensure unique
            if (offsetLookup.TryGetValue(key, out offset))
            {
                return offset;
            }

            // Write new
            offset = (OffsetIdx)offsets.Count;
            off = new Offset(offset, off.objectType, appended);
            offsetLookup.Add(key, offset);
            offsets.Add(off);
            return offset;
        }
        public OffsetIdx AppendOffset(OffsetIdx offset, NameIdx field)
        {
            nameArr[0] = field;
            return AppendOffset(offset, nameArr);
        }

        public void DefineGlobal(NameIdx name, bool isConstant, TypeIdx type, IEnumerable<Constant>? values = null)
        {
            if (!database.TryGetValue(name, out var entry))
            {
                throw new Exception();
            }

            if (entry.value.lookup != LookupIdx.LookupType.Identifier)
            {
                throw new Exception();
            }

            GetType(type);

            // Validate initializers
            constantBuf.Clear();
            if (values != null)
            {
                foreach (var value in values)
                {
                    Type valueType = GetType(value.type);
                    if (valueType.index == TypeIdx.Void)
                    {
                        if (!database.ContainsKey(value.value.global))
                        {
                            throw new Exception();
                        }
                    }
                    else if (valueType.index > TypeIdx.VPtr)
                    {
                        throw new Exception();
                    }
                    constantBuf.Add(value);
                }
            }

            // Ensure init count
            if (constantBuf.Count > Language.MaxInitializerCount)
            {
                throw new Exception();
            }

            // Write data
            DataTable dataTable = isConstant ? constants : globals;
            LookupIdx.LookupType lookupType = isConstant ? LookupIdx.LookupType.Constant : LookupIdx.LookupType.Global;
            database.UpdateValue(name, new LookupIdx { lookup = lookupType, value = new LookupIdx.Value { index = (Index)dataTable.info.Count } });

            dataTable.info.Add(new Field(name, type, (nuint)dataTable.data.Count));

            dataTable.data.Write((ushort)constantBuf.Count);
            foreach (var value in constantBuf)
            {
                dataTable.data.Write((byte)value.type);
                if (value.type == TypeIdx.Void)
                {
                    dataTable.data.Write(value.value.global);
                }
                else if (value.type != TypeIdx.VPtr)
                {
                    dataTable.data.WriteConstant(value);
                }
            }
        }
        public void DefineGlobal(NameIdx name, bool isConstant, TypeIdx type, Constant value)
        {
            constantArr[0] = value;
            DefineGlobal(name, isConstant, type, constantArr);
        }
        public NameIdx DefineGlobal(string name, bool isConstant, TypeIdx type, IEnumerable<Constant>? values = null)
        {
            NameIdx id = MakeIdentifier(name);
            DefineGlobal(id, isConstant, type, values);
            return id;
        }
        public NameIdx DefineGlobal(string name, bool isConstant, TypeIdx type, Constant value)
        {
            NameIdx id = MakeIdentifier(name);
            DefineGlobal(id, isConstant, type, constantArr);
            return id;
        }

        public TypeIdx DeclareType(NameIdx name)
        {
            if (!database.ContainsKey(name))
            {
                throw new Exception();
            }

            var entry = database[name];
            if (entry.value.lookup == LookupIdx.LookupType.Identifier)
            {
                TypeIdx index = (TypeIdx)types.Count;
                types.Add(new Type(name, index));
                database.UpdateValue(entry.key, new LookupIdx(index));
                return index;
            }
            else
            {
                if (entry.value.lookup != LookupIdx.LookupType.Type)
                {
                    throw new Exception();
                }

                return entry.value.value.type;
            }
        }
        public TypeIdx DeclareType(string name)
        {
            return DeclareType(MakeIdentifier(name));
        }
        public TypeWriter DefineType(TypeIdx type, bool isUnion = false)
        {
            var dst = GetType(type);
            if (dst.IsDefined || dst.writer != null)
            {
                throw new Exception();
            }

            dst.writer = new TypeWriter(this, dst.name, dst.index, isUnion);
            return dst.writer;
        }
        public TypeWriter DefineType(string name, bool isUnion = false)
        {
            return DefineType(DeclareType(MakeIdentifier(name)), isUnion);
        }

        public TypeIdx DeclarePointerType(TypeIdx baseType)
        {
            var type = GetType(baseType);
            if (type.pointerType == TypeIdx.Invalid)
            {
                // New pointer type
                TypeIdx idx = (TypeIdx)types.Count;
                Type generated = new(NameIdx.Invalid, idx);
                generated.MakePointer(baseType);
                generated.flags |= TypeFlags.IsDefined;
                type.pointerType = idx;
                types.Add(generated);
                return idx;
            }
            return type.pointerType;
        }
        public TypeIdx DeclareArrayType(TypeIdx baseType, nuint arraySize)
        {
            var type = GetType(baseType);
            if (type.arrayTypes == null || !type.arrayTypes.ContainsKey(arraySize))
            {
                // New array type
                TypeIdx idx = (TypeIdx)types.Count;
                Type generated = new(NameIdx.Invalid, idx);
                generated.MakeArray(baseType, arraySize);
                generated.flags |= TypeFlags.IsDefined;
                if (type.arrayTypes == null)
                {
                    type.arrayTypes = new Dictionary<nuint, TypeIdx>();
                }
                type.arrayTypes.Add(arraySize, idx);
                types.Add(generated);
                return idx;
            }
            return type.pointerType;
        }
        public TypeIdx DeclareSignatureType(SignatureIdx signature)
        {
            var sig = GetSignature(signature);
            if (sig.signatureType == TypeIdx.Invalid)
            {
                // New signature type
                TypeIdx idx = (TypeIdx)types.Count;
                Type generated = new(NameIdx.Invalid, idx);
                generated.MakeSignature(signature);
                generated.flags |= TypeFlags.IsDefined;
                sig.signatureType = idx;
                types.Add(generated);
                return idx;
            }
            return sig.signatureType;
        }

        public MethodIdx DeclareMethod(NameIdx name)
        {
            if (!database.ContainsKey(name))
            {
                throw new Exception();
            }

            var entry = database[name];
            if (entry.value.lookup == LookupIdx.LookupType.Identifier)
            {
                MethodIdx index = (MethodIdx)methods.Count;
                methods.Add(new Method(name, index));
                database.UpdateValue(entry.key, new LookupIdx(index));
                return index;
            }
            else
            {
                if (entry.value.lookup != LookupIdx.LookupType.Method)
                {
                    throw new Exception();
                }

                return entry.value.value.method;
            }
        }
        public MethodIdx DeclareMethod(string name)
        {
            return DeclareMethod(MakeIdentifier(name));
        }
        public MethodWriter DefineMethod(MethodIdx method, SignatureIdx signature)
        {
            var dst = GetMethod(method);
            if (dst.IsDefined || dst.writer != null)
            {
                throw new Exception();
            }

            Signature sig = GetSignature(signature);
            dst.writer = new MethodWriter(this, dst.name, dst.index, sig);
            return dst.writer;
        }
        public MethodWriter DefineMethod(string name, SignatureIdx signature)
        {
            return DefineMethod(DeclareMethod(MakeIdentifier(name)), signature);
        }

        public byte[] Export()
        {
            BinaryWriter exporter = new();

            // Insert header
            for (int i = 0; i < Language.IntermediateHeader.Length; i++)
            {
                exporter.Write((byte)Language.IntermediateHeader[i]);
            }
            exporter.Write(Platform.VersionNumber);

            // Write types
            var typeList = exporter.WriteDeferred();
            foreach (var type in types)
            {
                if (type.writer != null)
                {
                    type.writer.Export();
                }

                typeList.Write(type.name);
                typeList.Write(type.index);
                typeList.Write(type.flags);
                typeList.Write(type.generated);
                var fieldList = typeList.WriteDeferred();
                foreach (var field in type.fields)
                {
                    fieldList.Write(field.name);
                    fieldList.Write(field.type);
                    fieldList.Write(field.offset);
                    fieldList.arrayLength++;
                }
                typeList.Write(type.totalSize);
                typeList.Write(type.pointerType);
                typeList.Write(type.meta);
                typeList.Write(type.lineNumber);
                typeList.arrayLength++;
            }

            // Write methods
            var methodList = exporter.WriteDeferred();
            foreach (var method in methods)
            {
                if (method.writer != null)
                {
                    method.writer.Export();
                }

                methodList.Write(method.name);
                methodList.Write(method.index);
                methodList.Write(method.flags);
                methodList.Write(method.signature);
                methodList.WriteArray(method.bytecode);
                methodList.WriteArray(method.labels);
                var stackvarList = methodList.WriteDeferred();
                foreach (var stackvar in method.stackvars)
                {
                    stackvarList.Write(stackvar);
                    stackvarList.Write<nuint>(0); // offset
                    stackvarList.arrayLength++;
                }
                methodList.Write<nuint>(0); // total_size
                methodList.WriteArray(method.calls);
                methodList.WriteArray(method.globals);
                methodList.WriteArray(method.offsets);
                methodList.Write(method.meta);
                methodList.Write(method.lineNumber);
                methodList.arrayLength++;
            }

            // Write signatures
            var signatureList = exporter.WriteDeferred();
            foreach (var signature in signatures)
            {
                signatureList.Write(signature.index);
                signatureList.Write(signature.returnType);
                var parameterList = signatureList.WriteDeferred();
                foreach (var parameter in signature.parameters)
                {
                    parameterList.Write(parameter);
                    parameterList.Write<nuint>(0); // offset
                    parameterList.arrayLength++;
                }
                signatureList.Write<nuint>(0); // parameters_size
                signatureList.arrayLength++;
            }

            // Write offsets
            var offsetList = exporter.WriteDeferred();
            foreach (var offset in offsets)
            {
                offsetList.Write(offset.objectType);
                offsetList.WriteArray(offset.fieldNames);
                offsetList.Write(TypeIdx.Invalid); // type
                offsetList.Write<nuint>(0); // offset
                offsetList.arrayLength++;
            }

            // Write globals
            var infoList = exporter.WriteDeferred();
            foreach (var info in globals.info)
            {
                infoList.Write(info.name);
                infoList.Write(info.type);
                infoList.Write(info.offset);
                infoList.arrayLength++;
            }
            exporter.WriteArray(globals.data.ToArray());

            // Write constants
            infoList = exporter.WriteDeferred();
            foreach (var info in constants.info)
            {
                infoList.Write(info.name);
                infoList.Write(info.type);
                infoList.Write(info.offset);
                infoList.arrayLength++;
            }
            exporter.WriteArray(constants.data.ToArray());

            // Write database
            database.Export(exporter);

            // Write "metatable"
            var metaEntries = exporter.WriteDeferred();
            var metaString = exporter.WriteDeferred();
            if (meta != MetaIdx.Invalid)
            {
                byte[] fileNameBytes = System.Text.Encoding.UTF8.GetBytes(fileName);

                metaEntries.Write((Index)0); // off
                metaEntries.Write((Index)fileNameBytes.Length); // len
                metaEntries.Write(meta); // value
                metaEntries.arrayLength++;

                // string
                for (int i = 0; i < fileNameBytes.Length; i++)
                {
                    metaString.Write(fileNameBytes[i]);
                }
                metaString.arrayLength += fileNameBytes.Length;
            }

            // Append footer
            int footerLen = Language.Footer.Length;
            byte[] result = exporter.ToArray(footerLen);
            for (int i = 0, j = result.Length - footerLen; i < footerLen; i++, j++)
            {
                result[j] = (byte)Language.Footer[i];
            }

            return result;
        }


        private readonly Database<NameIdx, LookupIdx> database = new();
        private NameIdx EmplaceIdentifier(string name)
        {
            if (database.TryGetValue(name, out var entry))
            {
                return entry.key;
            }
            else
            {
                return database.Emplace(name, new LookupIdx
                {
                    lookup = LookupIdx.LookupType.Identifier,
                    value = new LookupIdx.Value
                    {
                        index = Language.InvalidIndex
                    }
                });
            }
        }

        private readonly List<Type> types = new();
        private Type GetType(TypeIdx type)
        {
            int idx = (int)type;
            if (idx >= types.Count)
            {
                throw new Exception();
            }
            return types[idx];
        }

        private readonly List<Method> methods = new();
        private Method GetMethod(MethodIdx method)
        {
            int idx = (int)method;
            if (idx >= methods.Count)
            {
                throw new Exception();
            }
            return methods[idx];
        }

        private readonly List<Signature> signatures = new();
        private readonly Dictionary<BinaryKey, SignatureIdx> signatureLookup = new();
        private Signature GetSignature(SignatureIdx signature)
        {
            int idx = (int)signature;
            if (idx >= signatures.Count)
            {
                throw new Exception();
            }
            return signatures[idx];
        }

        private readonly List<Offset> offsets = new();
        private readonly Dictionary<BinaryKey, OffsetIdx> offsetLookup = new();
        private Offset GetOffset(OffsetIdx offset)
        {
            int idx = (int)offset;
            if (idx >= offsets.Count)
            {
                throw new Exception();
            }
            return offsets[idx];
        }

        private class DataTable
        {
            public List<Field> info = new();
            public BinaryWriter data = new();
        }
        private readonly DataTable globals = new();
        private readonly DataTable constants = new();

        // Since the C# frontend cannot merge, there is no need for a metatable
        private readonly string fileName;
        private readonly MetaIdx meta;
        public Index lineNumber = 0;
    }
}
