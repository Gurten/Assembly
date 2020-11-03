using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Blamite.Blam;
using Blamite.Injection;
using Blamite.IO;
using Blamite.Util;
using NUnit.Framework;

//using SimpleJSON;

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

            {
                var buffer = new MemoryStream((int)config.RigidBodyTagBlock.Schema.Size);
                var bufferWriter = new EndianWriter(buffer, endianness);
                //bufferWriter.WriteBlock(block.Data);


                UInt16 val = mccReachContext.Get<IPhysicsModelShapeTypes>().Polyhedron.Value;
                Utils.WriteToStream(bufferWriter, config.RigidBodyTagBlock.Schema.ShapeIndex, val);

                var x = new byte[4]; // (int)config.RigidBodyTagBlock.Schema.ShapeIndex.Offset
                buffer.Seek((int)config.RigidBodyTagBlock.Schema.ShapeIndex.Offset, SeekOrigin.Begin);
                buffer.Read(x, 0, 4);
            }

            {
                var buffer = new MemoryStream((int)config.PolyhedraTagBlock.Schema.Size);
                var bufferWriter = new EndianWriter(buffer, endianness);
                Utils.WriteToStream(bufferWriter, config.PolyhedraTagBlock.Schema.AABBCenter, new float[] { 0.123f, 0.456f});

                var x = new byte[16]; // (int)config.RigidBodyTagBlock.Schema.ShapeIndex.Offset
                buffer.Seek((int)config.PolyhedraTagBlock.Schema.AABBCenter[0].Offset, SeekOrigin.Begin);
                buffer.Read(x, 0, 16);
            }

            {
                TagContainer container2 = new TagContainer();

                var buffer = new MemoryStream((int)config.PolyhedraTagBlock.Schema.Size);
                var bufferWriter = new EndianWriter(buffer, endianness);

                float radius = 123.0f;
                Serialization.Serialize(config.PolyhedraTagBlock.Schema, (aaa) => { aaa.Radius.Visit(bufferWriter, radius); });

                //last step.
                container2.AddTag(new ExtractedTag(new DatumIndex(), 0, CharConstant.FromString("phmo"), "synthesized_tag"));

                Utils.WriteToStream(bufferWriter, config.PolyhedraTagBlock.Schema.AABBCenter, new float[] { 0.123f, 0.456f });

                var x = new byte[16]; // (int)config.RigidBodyTagBlock.Schema.ShapeIndex.Offset
                buffer.Seek((int)config.PolyhedraTagBlock.Schema.AABBCenter[0].Offset, SeekOrigin.Begin);
                buffer.Read(x, 0, 16);

            }

            {
                TagContainer container2 = new TagContainer();

                var buffer = new MemoryStream((int)config.PolyhedraTagBlock.Schema.Size);
                var bufferWriter = new EndianWriter(buffer, endianness);

                float radius = 123.0f;
                Serialization.Serialize(config.PolyhedraTagBlock.Schema, (aaa) => { aaa.Radius.Visit(bufferWriter, radius); });

                //last step.
                container2.AddTag(new ExtractedTag(new DatumIndex(), 0, CharConstant.FromString("phmo"), "synthesized_tag"));
            }

            {
                var buffer = new MemoryStream((int)config.PolyhedraTagBlock.Schema.Size);
                var bufferWriter = new EndianWriter(buffer, endianness);

                var serializationContext = ContainerBuilder.CreateSerializationContext(config);
                var otherSC = serializationContext.CreateSerializationContext((phmo) => phmo.ListsShapesTagBlock);
                

                Utils.WriteToStream(bufferWriter, config.PolyhedraTagBlock.Schema.AABBCenter, new float[] { 0.123f, 0.456f });

            }





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
            { typeof(UInt64), 8 },
            { typeof(UInt32), 4 },
            { typeof(UInt16), 2 },
            { typeof(byte), 1 },
            { typeof(Blamite.Blam.StringID), 4 },
        };

        private static Dictionary<Type, Action<IWriter, object>> _typeWriters 
            = new Dictionary<Type, Action<IWriter, object>>()
        {
            { typeof(float), (IWriter writer, object obj) => writer.WriteFloat(value: (float)obj) },
            { typeof(UInt64), (IWriter writer, object obj) => writer.WriteUInt64(value: (UInt64)obj) },
            { typeof(UInt32), (IWriter writer, object obj) => writer.WriteUInt32(value: (UInt32)obj) },
            { typeof(UInt16), (IWriter writer, object obj) => writer.WriteUInt16(value: (UInt16)obj) },
            { typeof(byte), (IWriter writer, object obj) => writer.WriteByte(value: (byte)obj) },
            { typeof(Blamite.Blam.StringID), (IWriter writer, object obj) => writer.WriteUInt32(value: ((Blamite.Blam.StringID)obj).Value) },
        };

        private static Dictionary<Type, Func<IReader, object>> _typeReaders
           = new Dictionary<Type, Func<IReader, object>>()
       {
            { typeof(float), (IReader reader) => (object)reader.ReadFloat()},
            { typeof(UInt64), (IReader reader) => (object)reader.ReadUInt64() },
            { typeof(UInt32), (IReader reader) => (object)reader.ReadUInt32() },
            { typeof(UInt16), (IReader reader) => (object)reader.ReadUInt16() },
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
                writer.Invoke(buffer, (object)v);
                return; 
            }
            throw new NotImplementedException();
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
    }

    /*public class SerializedStruct<Struct> where Struct : IStructSchema
    {
        public SerializedStruct(Struct s)
        {
            _backing = new byte[(int)s.Size];
            _stream = new MemoryStream(_backing);
        }

        private readonly MemoryStream _stream;
        private readonly byte[] _backing;

    }*/


    public static class Serialization
    { 
        public static void Serialize<T>(T t, Action<T> action) where T : IStructSchema
        {


        }
    }


    public class SerializationBlob<T> where T : IStructSchema
    {
        public class SerializationContext<T>
        {
            public void Serialize(T t, Action<T> action)
            {


            }
        }

        public SerializationContext<T> Add()
        {
            return new SerializationContext<T>();
        }
    }


    public class ContainerBuilder
    {
        protected ContainerBuilder()
        { }

        protected UInt32 nextMockAddress = 1;

        public UInt32 GetNextMockAddress()
        {
            return nextMockAddress++;
        }
        

        public static SerializationContext<T> CreateSerializationContext<T>(T schema) where T : IStructSchema
        {
            return new SerializationContext<T>(schema, new ContainerBuilder());
        }


        public class SerializationContext<T> where T : IStructSchema
        {

            internal SerializationContext(T schema, ContainerBuilder builder)
            {
                _schema = schema;
            }

            /// <summary>
            /// Creates a Serialization context.
            /// Will create a SC for the relative tagblock.
            /// </summary>
            /// <typeparam name="U">The schema type of the relative tagblock</typeparam>
            /// <param name="action">A path to the tagblock from the schema.</param>
            /// <returns></returns>
            public SerializationContext<U> CreateSerializationContext<U>(Func<T, ITagBlockRef<U>> action) where U : IStructSchema
            {
                var tagblockRef = action.Invoke(_schema);
                UInt32 mockAddress = tagblockRef.Address.Visit(null);
                if (mockAddress != 0)
                { 
                    // TODO: find in the dictionary that's in ContainerBuilder, otherwise add a new data-store to the ContainerBuilder. 
                    // The members of the dict should be 
                }

                return new SerializationContext<U>(tagblockRef.Schema, _builder);
            }

            private T _schema;
            private ContainerBuilder _builder;




        }

    }






    public interface IStructSchema
    {
        UInt32 Size { get; }
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
            if (buffer.SeekTo(Offset) && buffer.Length <= (writeSizeBytes + Offset))
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
            for (uint i = 0, length = value.Length < Length? (uint)value.Length:Length;
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

    public interface ITagBlockRef<SchemaT>
    {
        DataField<UInt32> Count { get; }
        DataField<UInt32> Address { get; }

        SchemaT Schema { set;  get; }
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

    public struct ConfigConstant<T>
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
        ConfigConstant<UInt16> Polyhedron { get; }
        ConfigConstant<UInt16> List { get; }
    }

    interface IPhysicsModelMotionTypes
    {
        ConfigConstant<byte> Keyframed { get; }
        ConfigConstant<byte> Fixed { get; }
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
            ConfigConstant<UInt16> IPhysicsModelShapeTypes.Polyhedron => new ConfigConstant<UInt16>(4);
            ConfigConstant<UInt16> IPhysicsModelShapeTypes.List => new ConfigConstant<UInt16>(0xe);
        }

        public class PhysicsModelMotionTypes : IPhysicsModelMotionTypes
        {
            ConfigConstant<byte> IPhysicsModelMotionTypes.Keyframed => new ConfigConstant<byte>(4);

            ConfigConstant<byte> IPhysicsModelMotionTypes.Fixed => new ConfigConstant<byte>(5);
        }
    }

    interface IPhysicsModelMaterial : IStructSchema
    {
        DataField<Blamite.Blam.StringID> Name { get; }

        DataField<UInt16> PhantomTypeIndex { get;  }

    }

    class MCCReachPhysicsModelMaterial : IPhysicsModelMaterial
    {
        UInt32 IStructSchema.Size => 0x10;

        DataField<StringID> IPhysicsModelMaterial.Name => new DataField<StringID>(0);

        DataField<UInt16> IPhysicsModelMaterial.PhantomTypeIndex => new DataField<UInt16>(0xc);
    }

    interface IPhysicsModelPolyhedra : IStructSchema
    {
        DataField<byte> PhantomTypeIndex { get; }
        DataField<byte> CollisionGroup { get; }
        DataField<UInt16> Count { get; }
        // TODO: this is a different size in different engines.
        VectorField<byte> InstanceOffset { get; }
        DataField<float> Radius { get;  }
        VectorField<float> AABBHalfExtents { get; }
        VectorField<float> AABBCenter { get; }
        ISizeAndCapacityField FourVectors { get; }
        ISizeAndCapacityField PlaneEquations { get; }
    }

    class MCCReachPhysicsModelPolyhedra : IPhysicsModelPolyhedra
    {
        UInt32 IStructSchema.Size => 0xb0;

        DataField<byte> IPhysicsModelPolyhedra.PhantomTypeIndex => new DataField<byte>(0x1E);
        DataField<byte> IPhysicsModelPolyhedra.CollisionGroup => new DataField<byte>(0x1F);
        DataField<UInt16> IPhysicsModelPolyhedra.Count => new DataField<UInt16>(0x2A);
        /// <summary>
        /// This is a uint64 (8-bytes) in Halo Reach.
        /// </summary>
        VectorField<byte> IPhysicsModelPolyhedra.InstanceOffset => new VectorField<byte>(0x30, 8);
        DataField<float> IPhysicsModelPolyhedra.Radius => new DataField<float>(0x40);
        VectorField<float> IPhysicsModelPolyhedra.AABBHalfExtents => new VectorField<float>(0x50, 4);
        VectorField<float> IPhysicsModelPolyhedra.AABBCenter => new VectorField<float>(0x60, 4);
        ISizeAndCapacityField IPhysicsModelPolyhedra.FourVectors => new MCCReachSizeAndCapacityField(0x78);
        ISizeAndCapacityField IPhysicsModelPolyhedra.PlaneEquations => new MCCReachSizeAndCapacityField(0x98);
    }

    interface IPhysicsModelLists : IStructSchema
    {
        /// Some odd, poorly-named field. Happens to be '128'. 
        DataField<uint> Count { get; }
        ISizeAndCapacityField ChildShapes { get; }
        VectorField<float> AABBHalfExtents { get; }
        VectorField<float> AABBCenter { get; }
    }

    class MCCReachPhysicsModelLists : IPhysicsModelLists
    {
        UInt32 IStructSchema.Size => 0x90;

        DataField<uint> IPhysicsModelLists.Count => new DataField<uint>(0xA);
        ISizeAndCapacityField IPhysicsModelLists.ChildShapes => new MCCReachSizeAndCapacityField(0x38);
        VectorField<float> IPhysicsModelLists.AABBHalfExtents => new VectorField<float>(0x50, 4);
        VectorField<float> IPhysicsModelLists.AABBCenter => new VectorField<float>(0x60, 4);
    }

    interface IPhysicsModelListShapes : IStructSchema
    {
        DataField<UInt16> ShapeType { get; }
        DataField<UInt16> ShapeIndex { get; }
        DataField<UInt16> ChildShapeCount { get; }
    }

    class MCCReachPhysicsModelListShapes : IPhysicsModelListShapes
    {
        UInt32 IStructSchema.Size => 0x20;

        DataField<ushort> IPhysicsModelListShapes.ShapeType => new DataField<UInt16>(0);

        DataField<ushort> IPhysicsModelListShapes.ShapeIndex => new DataField<UInt16>(2);

        DataField<ushort> IPhysicsModelListShapes.ChildShapeCount => new DataField<UInt16>(0x10);
    }

    interface IPhysicsModelFourVectors : IStructSchema
    {
        VectorField<float> Vector0 {get;}
        VectorField<float> Vector1 {get;}
        VectorField<float> Vector2 {get;}
    }

    class PhysicsModelFourVectors : IPhysicsModelFourVectors
    {
        UInt32 IStructSchema.Size => 0x30;
        VectorField<float> IPhysicsModelFourVectors.Vector0 => new VectorField<float>(0, 4);

        VectorField<float> IPhysicsModelFourVectors.Vector1 => new VectorField<float>(0x10, 4);

        VectorField<float> IPhysicsModelFourVectors.Vector2 => new VectorField<float>(0x20, 4);
    }

    interface IPhysicsModelPlaneEquations : IStructSchema
    {
        VectorField<float> PlaneEquation { get; }
    }

    class PhysicsModelPlaneEquations : IPhysicsModelPlaneEquations
    {
        UInt32 IStructSchema.Size => 0x10;
        VectorField<float> IPhysicsModelPlaneEquations.PlaneEquation => new VectorField<float>(0, 4);
    }

    interface IPhysicsModelNode : IStructSchema
    {
        DataField<Blamite.Blam.StringID> Name { get; }
        DataField<UInt16> Flags { get; }
        DataField<UInt16> ParentIndex { get; }
        DataField<UInt16> SiblingIndex { get; }
        DataField<UInt16> ChildIndex { get; }
    }

    class PhysicsModelNode : IPhysicsModelNode
    {
        UInt32 IStructSchema.Size => 0xC;

        DataField<StringID> IPhysicsModelNode.Name => new DataField<StringID>(0);

        DataField<UInt16> IPhysicsModelNode.Flags => new DataField<UInt16>(4);

        DataField<UInt16> IPhysicsModelNode.ParentIndex => new DataField<UInt16>(6);

        DataField<UInt16> IPhysicsModelNode.SiblingIndex => new DataField<UInt16>(8);

        DataField<UInt16> IPhysicsModelNode.ChildIndex => new DataField<UInt16>(10);
    }

    interface IPhysicsModelRigidBody : IStructSchema
    {
        DataField<float> BoundingSphereRadius { get; }
        DataField<byte> MotionType { get; }
        DataField<UInt16> ShapeType { get; }
        DataField<float> Mass { get; }
        DataField<UInt16> ShapeIndex { get; }
    }

    class MCCReachPhysicsModelRigidBody : IPhysicsModelRigidBody
    {
        UInt32 IStructSchema.Size => 208;
        DataField<float> IPhysicsModelRigidBody.BoundingSphereRadius => new DataField<float>(20);
        DataField<byte> IPhysicsModelRigidBody.MotionType => new DataField<byte>(0x1c);
        DataField<UInt16> IPhysicsModelRigidBody.ShapeType => new DataField<UInt16>(168);
        DataField<float> IPhysicsModelRigidBody.Mass => new DataField<float>(0xB0);
        DataField<UInt16> IPhysicsModelRigidBody.ShapeIndex => new DataField<UInt16>(0xAA);
    }

    interface IPhysicsModel : IStructSchema
    { 
        ITagBlockRef<IPhysicsModelRigidBody> RigidBodyTagBlock { get; }
        ITagBlockRef<IPhysicsModelMaterial> MaterialsTagBlock { get; }
        ITagBlockRef<IPhysicsModelPolyhedra> PolyhedraTagBlock { get; }
        ITagBlockRef<IPhysicsModelFourVectors> PolyhedraFourVectorTagBlock { get; }
        ITagBlockRef<IPhysicsModelPlaneEquations> PolyhedraPlaneEquationsTagBlock { get; }
        ITagBlockRef<IPhysicsModelLists> ListsTagBlock { get; }
        ITagBlockRef<IPhysicsModelListShapes> ListsShapesTagBlock { get; }
        //ITagBlockRef<IPhysicsModelListShapes> RegionsTagBlock { get; }
        ITagBlockRef<IPhysicsModelNode> NodesTagBlock { get; }

    }

    class MCCReachPhysicsModel : IPhysicsModel
    {
        UInt32 IStructSchema.Size => 412;
        ITagBlockRef<IPhysicsModelRigidBody> IPhysicsModel.RigidBodyTagBlock 
            => new MCCReachTagBlockRef<IPhysicsModelRigidBody>(92, new MCCReachPhysicsModelRigidBody());

        ITagBlockRef<IPhysicsModelMaterial> IPhysicsModel.MaterialsTagBlock 
            => new MCCReachTagBlockRef<IPhysicsModelMaterial>(0x68, new MCCReachPhysicsModelMaterial());

        ITagBlockRef<IPhysicsModelPolyhedra> IPhysicsModel.PolyhedraTagBlock
            => new MCCReachTagBlockRef<IPhysicsModelPolyhedra>(0xb0, new MCCReachPhysicsModelPolyhedra());

        ITagBlockRef<IPhysicsModelFourVectors> IPhysicsModel.PolyhedraFourVectorTagBlock
            => new MCCReachTagBlockRef<IPhysicsModelFourVectors>(0xbc, new PhysicsModelFourVectors());

        ITagBlockRef<IPhysicsModelPlaneEquations> IPhysicsModel.PolyhedraPlaneEquationsTagBlock
            => new MCCReachTagBlockRef<IPhysicsModelPlaneEquations>(0xc8, new PhysicsModelPlaneEquations());

        ITagBlockRef<IPhysicsModelLists> IPhysicsModel.ListsTagBlock
            => new MCCReachTagBlockRef<IPhysicsModelLists>(0xe0, new MCCReachPhysicsModelLists());

        ITagBlockRef<IPhysicsModelListShapes> IPhysicsModel.ListsShapesTagBlock
            => new MCCReachTagBlockRef<IPhysicsModelListShapes>(0xe0, new MCCReachPhysicsModelListShapes());

        //ITagBlockRef<IPhysicsModelListShapes> IPhysicsModel.RegionsTagBlock => throw new NotImplementedException();

        ITagBlockRef<IPhysicsModelNode> IPhysicsModel.NodesTagBlock
            => new MCCReachTagBlockRef<IPhysicsModelNode>(0x13c, new PhysicsModelNode());
    }

    
}
