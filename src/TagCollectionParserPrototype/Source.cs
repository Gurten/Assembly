using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Blamite.Injection;
using Blamite.IO;
using Blamite.Util;
using NUnit.Framework;

namespace TagCollectionParserPrototype
{
    class Program
    {
        static void Main(string[] args)
        {
            IPhysicsModel config = new MCCReachPhysicsModel();
            string tagFilePath = @"C:\Users\gurten\Documents\tags\reach\ff_ramp_2x2_steep.tagc";
            Console.WriteLine("Hello World!");
            TagContainer container;

            // TODO: derrive endianness.
            var endianness = Blamite.IO.Endian.LittleEndian;

            using (var reader = new EndianReader(File.OpenRead(tagFilePath), endianness))
                container = TagContainerReader.ReadTagContainer(reader);

            Console.WriteLine(container.ToString());

            Assert.AreNotEqual(0, container.Tags.Count());
            var phmoTag = container.Tags.ElementAt(0);

            Assert.AreEqual(CharConstant.FromString("phmo"), phmoTag.Group, "First tag needs to be a phmo.");

            DataBlock tagData = container.FindDataBlock(phmoTag.OriginalAddress);
            Assert.IsNotNull(tagData, "could not find main tagblock");

            Assert.AreEqual(1, tagData.EntryCount);
            Assert.AreEqual(config.Size, tagData.EntrySize);

            var rigidBodyDataBlockFixup = tagData.AddressFixups.ElementAt(0);
            Assert.AreEqual(config.RigidBodyTagBlock.Address.Offset,
                rigidBodyDataBlockFixup.WriteOffset);

            var rigidBodyDataBlock = container.FindDataBlock(rigidBodyDataBlockFixup.OriginalAddress);
            Assert.IsNotNull(rigidBodyDataBlock, "could not find rigidbody tagblock");

            Assert.AreEqual(1, rigidBodyDataBlock.EntryCount);
            Assert.AreEqual(config.RigidBodyTagBlock.Schema.Size, rigidBodyDataBlock.EntrySize);

            IPhysicsModelPolyhedra poly = new MCCReachPhysicsModelPolyhedra();
            Assert.AreEqual(poly.AABBHalfExtents[3].Offset, 0x5c);

            IContext mccReachContext = new MCCReachContext();

            Assert.IsNotNull(mccReachContext.Get<IPhysicsModelShapeTypes>());

            var buffer = new MemoryStream((int)config.RigidBodyTagBlock.Schema.Size);
            var bufferWriter = new EndianWriter(buffer, endianness);
            //bufferWriter.WriteBlock(block.Data);


            UInt16 val = mccReachContext.Get<IPhysicsModelShapeTypes>().Polyhedron.Value.Value;
            Utils.WriteToStream(bufferWriter, config.RigidBodyTagBlock.Schema.ShapeIndex, (UInt16)val);

            var x = new byte[4];
            buffer.Read(x, (int)config.RigidBodyTagBlock.Schema.ShapeIndex.Offset, 4);
        }
    }

    // ===Ideas===
    // A tagblock schema should be useful enough to locate the field in an instance given a stream 
    // holding several instances. A subscript operator that gives you an interator. 


    // ======

    public class Utils
    {
        private static Dictionary<Type, UInt32> _typeSizes = new Dictionary<Type, UInt32>()
        {
            { typeof(float), 4 },
            { typeof(UInt32), 4 },
            { typeof(UInt16), 2 },
        };

        private static Dictionary<Type, Action<IWriter, object>> _typeWriters 
            = new Dictionary<Type, Action<IWriter, object>>()
        {
            { typeof(float), (IWriter writer, object obj) => writer.WriteFloat(value: (float)obj) },
            { typeof(UInt32), (IWriter writer, object obj) => writer.WriteUInt32(value: (UInt32)obj) },
            { typeof(UInt16), (IWriter writer, object obj) => writer.WriteUInt16(value: (UInt16)obj) },
        };

        public static UInt32 FieldSizeBytes<T, U>(T v) where T : ITypeBox<U>
        {
            UInt32 size = 0;
            if (_typeSizes.TryGetValue(typeof(U), out size))
            {
                return size;
            }

            throw new NotImplementedException();
        }

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
            // TODO: implement in similar way to dictionary for type sizes, except with 
            // delegates that write into the stream.
            throw new NotImplementedException();
        }

        //TODO: utilise this in the iterator. 
        public static bool WriteToStream<T, U>(IWriter buffer, DataField<U> field, T value) where T : struct, ITypeBox<U>
        {
            return WriteToStreamImpl(buffer, field.Offset, value);
        }

        public static bool WriteToStream<T>(IWriter buffer, DataField<T> field, T value) where T : struct
        {
            return WriteToStreamImpl(buffer, field.Offset, value);
        }

        private static bool WriteToStreamImpl<T>(IWriter buffer, UInt32 offset, T value) where T : struct
        {
            buffer.SeekTo(offset);
            UInt32 writeSizeBytes = Utils.FieldSizeBytes(value);
            if (buffer.Length <= (writeSizeBytes + offset))
            {
                Utils.WriteField(buffer, value);
            }
            return false;
        }


    }

    public interface IDataBlock
    {
        UInt32 Size { get; }
    }

    public class DataField<T> // where T : struct
    {
        public DataField(UInt32 offset)
        {
            Offset = offset;
            TypeStub = default(T);
        }
        public UInt32 Offset { protected set; get; }

        public T TypeStub { get;  }

    }

    interface IVectorField
    {
        UInt32 Length();
    }
    public abstract class VectorField<T> : IVectorField where T : struct
    {
        public VectorField(UInt32 baseOffsetInParent)
        {
            _baseOffsetInParent = baseOffsetInParent;
        }

        private UInt32 _baseOffsetInParent;

        public abstract UInt32 Length();

        public DataField<T> this[UInt32 i]
        {
            get => i >= Length() ? throw new IndexOutOfRangeException() :
                new DataField<T>(_baseOffsetInParent + i * Utils.FieldSizeBytes(default(T)));
        }
    }

    public class Vector4Field<T> : VectorField<T> where T : struct
    {
        public Vector4Field(UInt32 baseOffsetInParent) : base(baseOffsetInParent) { }
        public override UInt32 Length() { return 4; }
    }


    interface ISizeAndCapacityField
    {
        DataField<UInt32> Size { get; }
        DataField<UInt32> Capacity { get; }
    }

    /// <summary>
    /// Grouping of size and capacity.
    /// 
    /// Decided to group these because capacity is often size + 0x80000000 in practice.
    /// </summary>
    public class MCCReachSizeAndCapacityField : ISizeAndCapacityField
    { 
        public MCCReachSizeAndCapacityField(UInt32 baseOffsetInParent)
        {
            _baseOffsetInParent = baseOffsetInParent;
        }

        private UInt32 _baseOffsetInParent;

        DataField<UInt32> ISizeAndCapacityField.Size => new DataField<UInt32>(_baseOffsetInParent);
        DataField<UInt32> ISizeAndCapacityField.Capacity => new DataField<UInt32>(_baseOffsetInParent + 4);
    }

    interface ITagBlockRef<SchemaT>
    {
        DataField<UInt32> Count { get; }
        DataField<UInt32> Address { get; }

        SchemaT Schema { set;  get; }
    }

    public class MCCReachTagBlockRef<T> : ITagBlockRef<T>
    {
        public MCCReachTagBlockRef(UInt32 offsetInParent, T instance)
        {
            Schema = instance;
            _offsetInParent = offsetInParent;
        }

        UInt32 _offsetInParent = 0;
        public DataField<UInt32> Count => new DataField<UInt32>(_offsetInParent + 0);

        public DataField<UInt32> Address => new DataField<UInt32>(_offsetInParent + 4);

        public T Schema { get; set; }
    }

    public interface ITypeBox
    { }

    public interface ITypeBox<T> : ITypeBox
    { 
        T Value { get; }
    }

    public struct PhysicsModelShapeType : ITypeBox<UInt16>
    {
        public PhysicsModelShapeType(UInt16 value)
        {
            _value = value;
        }

        private UInt16 _value;

        public UInt16 Value { get => _value; }

        /*public static implicit operator PhysicsModelShapeType(UInt16 value)
        {
            return new PhysicsModelShapeType(value);
        }*/
    }

    public class ConfigConstant<T>
    {
        public ConfigConstant(T val)
        {
            Value = val;
        }

        public static implicit operator T(ConfigConstant<T> value)
        {
            return value.Value;
        }

        public T Value { get; private set; }

    }

    interface IContext
    {
        T Get<T>();
    }

    interface IPhysicsModelShapeTypes
    {
        //Add more as needed.
        ConfigConstant<PhysicsModelShapeType> Polyhedron { get; }
        ConfigConstant<PhysicsModelShapeType> List { get; }
    }

    public class MCCReachContext : IContext
    {
        private Dictionary<Type, object> _handlers = new Dictionary<Type, object>()
        {
            { typeof(IPhysicsModelShapeTypes), new PhysicsModelShapeTypes() },
        };

        public T Get<T>()
        {
            object obj = 0;
            if (_handlers.TryGetValue(typeof(T), out obj))
            {
                return (T)obj;
            }

            throw new NotImplementedException();
        }

        public class PhysicsModelShapeTypes : IPhysicsModelShapeTypes
        {
            ConfigConstant<PhysicsModelShapeType> IPhysicsModelShapeTypes.Polyhedron => new ConfigConstant<PhysicsModelShapeType>(4);
            ConfigConstant<PhysicsModelShapeType> IPhysicsModelShapeTypes.List => new ConfigConstant<PhysicsModelShapeType>(0xe);
        }
    }

    interface IPhysicsModelRigidBody : IDataBlock
    {
        DataField<float> BoundingSphereRadius { get; }
        DataField<UInt16> ShapeTypeOffset { get; }
        DataField<PhysicsModelShapeType> ShapeIndex { get;  }
    }

    interface IPhysicsModelMaterial : IDataBlock
    {
    }

    class MCCReachPhysicsModelMaterial : IPhysicsModelMaterial
    {
        UInt32 IDataBlock.Size => 0x10;
    }

    interface IPhysicsModelPolyhedra : IDataBlock
    {
        DataField<float> Radius { get;  }
        Vector4Field<float> AABBHalfExtents { get; }
        Vector4Field<float> AABBCenter { get; }
        ISizeAndCapacityField FourVectors { get; }
        ISizeAndCapacityField PlaneEquations { get; }
    }

    class MCCReachPhysicsModelPolyhedra : IPhysicsModelPolyhedra
    {
        UInt32 IDataBlock.Size => 0xb0;

        DataField<float> IPhysicsModelPolyhedra.Radius => new DataField<float>(0x40);
        Vector4Field<float> IPhysicsModelPolyhedra.AABBHalfExtents => new Vector4Field<float>(0x50);
        Vector4Field<float> IPhysicsModelPolyhedra.AABBCenter => new Vector4Field<float>(0x60);

        ISizeAndCapacityField IPhysicsModelPolyhedra.FourVectors => new MCCReachSizeAndCapacityField(0x78);

        ISizeAndCapacityField IPhysicsModelPolyhedra.PlaneEquations => new MCCReachSizeAndCapacityField(0x98);
    }

    interface IPhysicsModelLists : IDataBlock
    {
        /// Some odd, poorly-named field. Happens to be '128'. 
        DataField<uint> Count { get; }
        ISizeAndCapacityField ChildShapes { get; }
        Vector4Field<float> AABBHalfExtents { get; }
        Vector4Field<float> AABBCenter { get; }
    }

    class MCCReachPhysicsModelLists : IPhysicsModelLists
    {
        UInt32 IDataBlock.Size => 0x90;

        DataField<uint> IPhysicsModelLists.Count => new DataField<uint>(0xA);
        ISizeAndCapacityField IPhysicsModelLists.ChildShapes => new MCCReachSizeAndCapacityField(0x38);
        Vector4Field<float> IPhysicsModelLists.AABBHalfExtents => new Vector4Field<float>(0x50);
        Vector4Field<float> IPhysicsModelLists.AABBCenter => new Vector4Field<float>(0x60);
    }

    interface IPhysicsModelListShapes : IDataBlock
    {

    }

    class MCCReachPhysicsModelListShapes : IPhysicsModelListShapes
    {
        UInt32 IDataBlock.Size => 0x20;
    }

    interface IPhysicsModelFourVectors : IDataBlock
    {
        Vector4Field<float> Vector0 {get;}
        Vector4Field<float> Vector1 {get;}
        Vector4Field<float> Vector2 {get;}
    }

    class PhysicsModelFourVectors : IPhysicsModelFourVectors
    {
        UInt32 IDataBlock.Size => 0x30;
        Vector4Field<float> IPhysicsModelFourVectors.Vector0 => new Vector4Field<float>(0);

        Vector4Field<float> IPhysicsModelFourVectors.Vector1 => new Vector4Field<float>(0x10);

        Vector4Field<float> IPhysicsModelFourVectors.Vector2 => new Vector4Field<float>(0x20);
    }

    interface IPhysicsModelPlaneEquations : IDataBlock
    {
        Vector4Field<float> PlaneEquation { get; }
    }

    class PhysicsModelPlaneEquations : IPhysicsModelPlaneEquations
    {
        UInt32 IDataBlock.Size => 0x10;
        Vector4Field<float> IPhysicsModelPlaneEquations.PlaneEquation => new Vector4Field<float>(0);
    }

    class MCCReachPhysicsModelRigidBody : IPhysicsModelRigidBody
    {
        UInt32 IDataBlock.Size => 208;
        DataField<float> IPhysicsModelRigidBody.BoundingSphereRadius => new DataField<float>(20);
        DataField<UInt16> IPhysicsModelRigidBody.ShapeTypeOffset => new DataField<UInt16>(168);
        DataField<PhysicsModelShapeType> IPhysicsModelRigidBody.ShapeIndex => new DataField<PhysicsModelShapeType>(0xAA);
    }

    interface IPhysicsModel : IDataBlock
    { 
        ITagBlockRef<IPhysicsModelRigidBody> RigidBodyTagBlock { get; }

    }

    class MCCReachPhysicsModel : IPhysicsModel
    {
        UInt32 IDataBlock.Size => 412;
        ITagBlockRef<IPhysicsModelRigidBody> IPhysicsModel.RigidBodyTagBlock 
            => new MCCReachTagBlockRef<IPhysicsModelRigidBody>(92, new MCCReachPhysicsModelRigidBody());

    }
}
