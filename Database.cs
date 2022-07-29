using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Propane
{
    internal class Database<TKey, TValue> where TKey : unmanaged where TValue : unmanaged
    {
        public struct Entry
        {
            public TKey key;
            public TValue value;
            public string name;
        }

        private readonly List<Entry> entries = new();
        private readonly Dictionary<string, int> lookup = new();

        private static int KeyToInt(TKey key) => Unsafe.As<TKey, int>(ref key);
        private static TKey IntToKey(int i) => Unsafe.As<int, TKey>(ref i);

        public Entry this[TKey index] => entries[KeyToInt(index)];
        public Entry this[string name]
        {
            get
            {
                if (lookup.TryGetValue(name, out int index))
                {
                    return entries[index];
                }

                throw new KeyNotFoundException();
            }
        }

        public bool TryGetValue(string name, out Entry entry)
        {
            if (lookup.TryGetValue(name, out int index))
            {
                entry = entries[index];
                return true;
            }

            entry = default;
            return false;
        }
        public bool TryGetValue(TKey key, out Entry entry)
        {
            if (ContainsKey(key))
            {
                entry = entries[KeyToInt(key)];
                return true;
            }

            entry = default;
            return false;
        }

        public bool ContainsKey(TKey key)
        {
            int index = KeyToInt(key);
            return index >= 0 && index < entries.Count;
        }
        public bool ContainsName(string name) => lookup.ContainsKey(name);

        public TKey Emplace(string name, TValue value)
        {
            if (lookup.TryGetValue(name, out int index))
            {
                var replace = entries[index];
                replace.value = value;
                entries[index] = replace;
                return IntToKey(index);
            }
            else
            {
                index = entries.Count;
                TKey key = IntToKey(index);
                lookup.Add(name, index);
                entries.Add(new Entry { key = key, value = value, name = name });
                return key;
            }
        }
        public void UpdateValue(TKey key, TValue value)
        {
            int index = KeyToInt(key);
            var replace = entries[index];
            replace.value = value;
            entries[index] = replace;
        }

        public int Count => entries.Count;

        public void Export(BinaryWriter writer)
        {
            BinaryWriter entriesList = writer.WriteDeferred();
            BinaryWriter strings = writer.WriteDeferred();
            foreach (var entry in entries)
            {
                entriesList.Write((Index)strings.Count);
                entriesList.Write((Index)entry.name.Length);
                entriesList.Write(entry.key);
                entriesList.Write(entry.value);
                entriesList.arrayLength++;

                for (int i = 0; i < entry.name.Length; i++)
                {
                    strings.Write((byte)entry.name[i]);
                }
                strings.arrayLength += entry.name.Length;
            }
        }
    }
}
