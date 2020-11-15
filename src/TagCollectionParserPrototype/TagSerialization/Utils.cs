/// The Tag Collection Parser Prototype Project
/// Author: Gurten
using Blamite.IO;
using System;
using System.Collections.Generic;
using System.IO;
using TagCollectionParserPrototype.Schema.Core;

namespace TagCollectionParserPrototype.TagSerialization
{
    public static class Utils
    {
        private static Dictionary<Type, UInt32> _typeSizes = new Dictionary<Type, UInt32>()
        {
            { typeof(float), 4 },
            { typeof(UInt64), 8 },
            { typeof(UInt32), 4 },
            { typeof(Int32), 4 },
            { typeof(UInt16), 2 },
            { typeof(Int16), 2 },
            { typeof(byte), 1 },
            { typeof(Blamite.Blam.StringID), 4 },
        };

        private static Dictionary<Type, Action<IWriter, object>> _typeWriters
            = new Dictionary<Type, Action<IWriter, object>>()
        {
            { typeof(float), (IWriter writer, object obj) => writer.WriteFloat(value: (float)obj) },
            { typeof(UInt64), (IWriter writer, object obj) => writer.WriteUInt64(value: (UInt64)obj) },
            { typeof(UInt32), (IWriter writer, object obj) => writer.WriteUInt32(value: (UInt32)obj) },
            { typeof(Int32), (IWriter writer, object obj) => writer.WriteInt32(value: (Int32)obj) },
            { typeof(UInt16), (IWriter writer, object obj) => writer.WriteUInt16(value: (UInt16)obj) },
            { typeof(Int16), (IWriter writer, object obj) => writer.WriteInt16(value: (Int16)obj) },
            { typeof(byte), (IWriter writer, object obj) => writer.WriteByte(value: (byte)obj) },
            { typeof(Blamite.Blam.StringID), (IWriter writer, object obj) => writer.WriteUInt32(value: ((Blamite.Blam.StringID)obj).Value) },
        };

        private static Dictionary<Type, Func<IReader, object>> _typeReaders
           = new Dictionary<Type, Func<IReader, object>>()
       {
            { typeof(float), (IReader reader) => (object)reader.ReadFloat()},
            { typeof(UInt64), (IReader reader) => (object)reader.ReadUInt64() },
            { typeof(UInt32), (IReader reader) => (object)reader.ReadUInt32() },
            { typeof(Int32), (IReader reader) => (object)reader.ReadInt32() },
            { typeof(UInt16), (IReader reader) => (object)reader.ReadUInt16() },
            { typeof(Int16), (IReader reader) => (object)reader.ReadInt16() },
            { typeof(byte), (IReader reader) => (object)reader.ReadByte() },
       };

        public static UInt32 FieldSizeBytes<T>(T v) where T : struct
        {
            UInt32 size = 0;
            if (_typeSizes.TryGetValue(typeof(T), out size))
            {
                return size;
            }
            throw new NotImplementedException();
        }

        public static void WriteField<T>(IWriter buffer, T v) where T : struct
        {
            Action<IWriter, object> writer;
            if (_typeWriters.TryGetValue(typeof(T), out writer))
            {
                writer.Invoke(buffer, v);
                return;
            }
            throw new NotImplementedException(
                v.GetType().ToString() + " cannot be serialized.");
        }

        public static T ReadField<T>(IReader buffer) where T : struct
        {
            Func<IReader, object> reader;
            if (_typeReaders.TryGetValue(typeof(T), out reader))
            {
                return (T)reader.Invoke(buffer);
            }
            throw new NotImplementedException();
        }

        //TODO: utilise this in the iterator. 
        public static bool WriteToStream<T>(IWriter buffer, IDataField<T> field, T value)
        {
            return field.Visit(buffer, value);
        }

        public static UInt32 GetAlignedSize(IStructSchema schema)
        {
            return GetAlignedSize(schema.Size, schema.Alignment);
        }

        public static UInt32 GetAlignedSize(UInt32 effectiveSize, UInt32 alignment)
        {
            UInt32 paddingBytes = 0;
            if (alignment > 0)
            {
                // Power of 2 check
                if (!((alignment & (alignment - 1)) == 0))
                {
                    throw new InvalidDataException("Alignment needs to be power of 2.");
                }
                UInt32 mask = alignment - 1;
                paddingBytes = (alignment - (effectiveSize & mask)) & mask;
            }
            return effectiveSize + paddingBytes;
        }
    }
}
