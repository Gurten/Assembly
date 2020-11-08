/// The Tag Collection Parser Prototype Project
/// Author: Gurten
using Blamite.IO;
using System;
using TagCollectionParserPrototype.Schema.Core;

namespace TagCollectionParserPrototype.Schema.MccReach.Core
{
    /// <summary>
    /// Grouping of size and capacity.
    /// 
    /// Decided to group these because capacity is often size + 0x80000000 in practice.
    /// </summary>
    public struct MCCReachSizeAndCapacityField : ISizeAndCapacityField
    {
        public MCCReachSizeAndCapacityField(UInt32 baseOffsetInParent)
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

    public struct MCCReachTagBlockRef<T> : ITagBlockRef<T>
    {
        public MCCReachTagBlockRef(UInt32 offsetInParent, T schema)
        {
            Schema = schema;
            _offsetInParent = offsetInParent;
        }

        private readonly UInt32 _offsetInParent;
        public DataField<UInt32> Count => new DataField<UInt32>(_offsetInParent + 0);

        public DataField<UInt32> Address => new DataField<UInt32>(_offsetInParent + 4);

        public T Schema { get; set; }
    }
}
