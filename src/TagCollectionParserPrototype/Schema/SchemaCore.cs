/// The Tag Collection Parser Prototype Project
/// Author: Gurten
using Blamite.IO;
using System;
using TagCollectionParserPrototype.TagSerialization;

namespace TagCollectionParserPrototype.Schema.Core
{
    public interface IStructSchema
    {
        UInt32 Size { get; }
        UInt32 Alignment { get; }
    }

    public interface IStructWithDataFixup
    {
        void VisitInstance(IWriter writer, UInt32 index);
    }

    public interface IDataField<BackingType>
    {
        bool Visit(IWriter buffer, BackingType value);
        BackingType Visit(IReader buffer);
    }

    public struct DataField<T> : IDataField<T> where T : struct
    {
        public DataField(UInt32 offset)
        {
            Offset = offset;
        }

        public readonly UInt32 Offset;

        public bool Visit(IWriter buffer, T value)
        {
            UInt32 writeSizeBytes = Utils.FieldSizeBytes(value);
            UInt32 bufferLength = (UInt32)buffer.Length;
            if (buffer.SeekTo(Offset) && (writeSizeBytes + Offset) <= bufferLength)
            {
                Utils.WriteField(buffer, value);
                return true;
            }
            return false;
        }

        public T Visit(IReader buffer)
        {
            buffer.SeekTo(Offset);
            return Utils.ReadField<T>(buffer);
        }
    }

    public struct VectorField<T> : IDataField<T[]> where T : struct
    {
        public VectorField(UInt32 baseOffsetInParent, UInt32 length)
        {
            _baseOffsetInParent = baseOffsetInParent;
            Length = length;
        }

        private UInt32 _baseOffsetInParent;

        public readonly UInt32 Length;


        public bool Visit(IWriter buffer, T[] value)
        {
            for (uint i = 0, length = value.Length < Length ? (uint)value.Length : Length;
                i < length; ++i)
            {
                if (this[i].Visit(buffer, value[i]))
                {
                    continue;
                }
                return false;
            }

            return false;
        }

        public T[] Visit(IReader buffer)
        {
            var output = new T[Length];
            for (uint i = 0; i < Length; ++i)
            {
                output[i] = this[i].Visit(buffer);
            }

            return output;
        }

        public DataField<T> this[UInt32 i]
        {
            get => i >= Length ? throw new IndexOutOfRangeException() :
                new DataField<T>(_baseOffsetInParent + i * Utils.FieldSizeBytes(default(T)));
        }
    }

    interface ISizeAndCapacityField : IDataField<UInt32>
    {
        DataField<UInt32> Size { get; }
        DataField<UInt32> Capacity { get; }
    }

    public interface ITagBlockRef
    {
        DataField<UInt32> Count { get; }
        DataField<UInt32> Address { get; }
    }

    public interface ITagBlockRef<SchemaT> : ITagBlockRef
    {
        SchemaT Schema { set; get; }
    }

    /// <summary>
    /// Used to decorate the root tagblock type.
    /// </summary>
    public interface ITagRoot
    { }
}
