using System;
using System.Collections.Generic;

namespace Propane
{
    using Index = UInt32;

    internal class BinaryWriter
    {
        public BinaryWriter()
        {
            buffer = new byte[1024];
        }
        private BinaryWriter(int offset)
        {
            buffer = new byte[1024];
            parentOffset = offset;
        }

        public int Count => pos;
        public byte[] Data => buffer;

        public void Clear()
        {
            pos = 0;
        }

        public byte[] ToArray(int padding = 0)
        {
            if (children != null)
            {
                foreach (var child in children)
                {
                    Index writeOffset = (Index)pos;
                    byte[] childData = child.ToArray();

                    EnsureCapacity(pos + childData.Length);
                    Array.Copy(childData, 0, buffer, pos, childData.Length);
                    pos += childData.Length;

                    unsafe
                    {
                        fixed (byte* ptr = &buffer[child.parentOffset])
                        {
                            Index* asIdx = (Index*)ptr;
                            *asIdx++ = writeOffset - (Index)child.parentOffset;
                            *asIdx++ = (Index)child.arrayLength;
                        }
                    }
                }
                children = null;
            }

            byte[] data = new byte[pos + padding];
            Array.Copy(buffer, 0, data, 0, pos);
            return data;
        }

        public void Write<T>(T value) where T : unmanaged
        {
            unsafe
            {
                int size = sizeof(T);
                EnsureCapacity(pos + size);
                fixed (byte* ptr = &buffer[pos])
                {
                    *(T*)ptr = value;
                }
                pos += size;
            }
        }
        public void WriteArray<T>(T[] value) where T : unmanaged
        {
            var child = WriteDeferred();
            child.arrayLength += value.Length;
            for (int i = 0; i < value.Length; i++)
            {
                child.Write(value[i]);
            }
        }
        public BinaryWriter WriteDeferred()
        {
            int offset = pos;
            Write((Index)0);
            Write((Index)0);
            if (children == null)
            {
                children = new();
            }
            BinaryWriter child = new(offset);
            children.Add(child);
            return child;
        }

        private void EnsureCapacity(int capacity)
        {
            if (buffer.Length < capacity)
            {
                int newCapacity = Math.Max(capacity, buffer.Length * 2);
                Array.Resize(ref buffer, newCapacity);
            }
        }

        private byte[] buffer;
        private int pos = 0;

        private readonly int parentOffset = 0;
        public int arrayLength = 0;

        private List<BinaryWriter>? children = null;
    }
}
