/// The Tag Collection Parser Prototype Project
/// Author: Gurten
using Blamite.IO;
using System;
using System.IO;
using TagCollectionParserPrototype.Cache.Core;
using TagCollectionParserPrototype.TagSerialization;

namespace TagCollectionParserPrototype.Schema.Core
{
    /// <summary>
    /// Grouping of size and capacity.
    /// 
    /// Decided to group these because capacity is often size + 0x80000000 in practice.
    /// </summary>
    public struct NewGenSizeAndCapacityField : ISizeAndCapacityField
    {
        public NewGenSizeAndCapacityField(UInt32 baseOffsetInParent)
        {
            _baseOffsetInParent = baseOffsetInParent;
        }

        private UInt32 _baseOffsetInParent;

        public DataField<UInt32> Size => new DataField<UInt32>(_baseOffsetInParent);
        public DataField<UInt32> Capacity => new DataField<UInt32>(_baseOffsetInParent + 4);

        public bool Visit(IWriter buffer, uint value)
        {
            const uint valueAdjustment = 0x80000000;
            return Size.Visit(buffer, value) && Capacity.Visit(buffer, value | valueAdjustment);
        }

        public UInt32 Visit(IReader buffer)
        {
            buffer.SeekTo(Size.Offset);
            return Utils.ReadField<UInt32>(buffer);
        }

    }

    public struct NewGenTagBlockRef<T> : ITagBlockRef<T>
    {
        public NewGenTagBlockRef(UInt32 offsetInParent, T schema)
        {
            Schema = schema;
            _offsetInParent = offsetInParent;
        }

        private readonly UInt32 _offsetInParent;
        public DataField<UInt32> Count => new DataField<UInt32>(_offsetInParent + 0);

        public DataField<UInt32> Address => new DataField<UInt32>(_offsetInParent + 4);

        public T Schema { get; set; }
    }

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

    public interface IVectorField<T> : IDataField<T[]> where T : struct
    {
        void Visit<U>(IWriter buffer, ConfigConstant<U> constant) where U : struct;
    }

    public struct VectorField<T> : IVectorField<T> where T : struct
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

        public void Visit<U>(IWriter buffer, ConfigConstant<U> constant) where U : struct
        {
            UInt32 uByteCount = Utils.FieldSizeBytes(constant.Value);
            UInt32 thisFieldByteCount = Utils.FieldSizeBytes(default(T)) * Length;
            buffer.SeekTo(this[0].Offset);

            if (thisFieldByteCount < uByteCount)
            {
                UInt32 sizeMismatchByteCount = uByteCount - thisFieldByteCount;
                byte[] patch = new byte[uByteCount];
                var patchBuffer = new EndianWriter(new MemoryStream(patch), buffer.Endianness);
                Utils.WriteField(patchBuffer, constant.Value);
                UInt32 beginReadingOffset 
                    = buffer.Endianness==Endian.LittleEndian? 
                    0 : sizeMismatchByteCount;
                // Begin writing where we'll discard the most-significant bytes. 
                // These should just be padding anyway, but we check to be sure.
                buffer.WriteBlock(patch, (int)beginReadingOffset, (int)thisFieldByteCount);
                for (UInt32 i = 0; i < sizeMismatchByteCount; ++i)
                {
                    UInt32 excessByteIndex = (i + beginReadingOffset + thisFieldByteCount) 
                        % uByteCount;
                    byte b = patch[(int)excessByteIndex];
                    if (b != 0)
                    {
                        throw new OverflowException("Written data truncated. byte " 
                            + b + " in " + constant.Value);
                    }
                }
            }
            else
            {
                Utils.WriteField(buffer, constant.Value);
            }
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
