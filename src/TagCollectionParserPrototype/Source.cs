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

            ICacheContext context = new MCCReachContext();

            Assert.IsNotNull(context.Get<IPhysicsModelShapeTypes>());

            {
                var buffer = new MemoryStream((int)config.RigidBodyTagBlock.Schema.Size);
                var bufferWriter = new EndianWriter(buffer, endianness);
                //bufferWriter.WriteBlock(block.Data);


                UInt16 val = context.Get<IPhysicsModelShapeTypes>().Polyhedron.Value;
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
                var sc = ContainerBuilder.CreateSerializationContext(config, context);
                {
                    var rigidBodySc = sc.GetSerializationContext((phmo) => phmo.RigidBodyTagBlock);
                    rigidBodySc.Add().Serialize((writer, a) =>
                    {
                        a.BoundingSphereRadius.Visit(writer, 123f);
                        a.MotionType.Visit(writer, context.Get<IPhysicsModelMotionTypes>().Fixed);
                        a.ShapeType.Visit(writer, context.Get<IPhysicsModelShapeTypes>().List);
                        a.Mass.Visit(writer, 123f);
                        a.ShapeIndex.Visit(writer, 0);
                    });
                }
                { 
                    var listSc = sc.GetSerializationContext((phmo) => phmo.ListsTagBlock);
                    listSc.Add().Serialize((writer, a) => {
                        a.AABBHalfExtents.Visit(writer, new float[] {1f, 1f, 1f, 0.01639998f });
                        a.AABBCenter.Visit(writer, new float[] {0f, 0f, 1f, 1f });
                        a.ChildShapes.Visit(writer, 3); // put the actual value here.
                    });
                }

                {
                    var listShapesSc = sc.GetSerializationContext((phmo) => phmo.ListsShapesTagBlock);
                    for (UInt16 i = 0; i < 3; ++i)
                    {
                        listShapesSc.Add().Serialize((writer, a) => {
                            a.ShapeIndex.Visit(writer, i);
                            a.ChildShapeCount.Visit(writer, 3);
                            a.ShapeType.Visit(writer, context.Get<IPhysicsModelShapeTypes>().Polyhedron);
                        });
                    }
                }

                {
                    var polyhedraSc = sc.GetSerializationContext((phmo) => phmo.PolyhedraTagBlock);
                    var planesSc = sc.GetSerializationContext((phmo) => phmo.PolyhedraPlaneEquationsTagBlock);
                    var fourVectorSc = sc.GetSerializationContext((phmo) => phmo.PolyhedraFourVectorTagBlock);

                    polyhedraSc.Add().Serialize((writer, a) => {
                        a.Name.Visit(writer, new StringID(1)); // default
                        a.Radius.Visit(writer, 0.0164f);
                        a.FourVectors.Visit(writer, 2);
                        a.PlaneEquations.Visit(writer, 6);
                        a.VertexCount.Visit(writer, 6);
                        a.AABBHalfExtents.Visit(writer, new float[] { 0.9835999f, 0.08360004f, 0.9836004f, 0f });
                        a.AABBCenter.Visit(writer, new float[] { 0f, -0.9f, 1f, 0f});
                    });

                    planesSc.Add().Serialize((writer, a) => {
                        a.PlaneEquation.Visit(writer, new float[] { 0f, -1f, 0f, -0.9836001f });
                    });
                    planesSc.Add().Serialize((writer, a) => {
                        a.PlaneEquation.Visit(writer, new float[] { 0f, 1f, 0f, 0.8164f });
                    });
                    planesSc.Add().Serialize((writer, a) => {
                        a.PlaneEquation.Visit(writer, new float[] { -0.6944277f, 0.1885228f, 0.6944273f, -0.5089993f });
                    });
                    planesSc.Add().Serialize((writer, a) => {
                        a.PlaneEquation.Visit(writer, new float[] { -0.6826782f, 0.2465233f, 0.6878784f, -0.4505166f });
                    });
                    planesSc.Add().Serialize((writer, a) => {
                        a.PlaneEquation.Visit(writer, new float[] { 1f, 0f, 0f, -0.9835998f });
                    });
                    planesSc.Add().Serialize((writer, a) => {
                        a.PlaneEquation.Visit(writer, new float[] { 0f, 0f, -1f, 0.01640397f });
                    });

                    fourVectorSc.Add().Serialize((writer, a) => {
                        a.Vector0.Visit(writer, new float[] { 0.9835998f, 0.9835998f, 0.9835998f, 0.9835999f });
                        a.Vector1.Visit(writer, new float[] { -0.8164001f, -0.8164001f, -0.9835998f, -0.9835998f });
                        a.Vector2.Visit(writer, new float[] { 1.923637f, 0.01640403f, 1.983605f, 0.01640403f });
                    });

                    fourVectorSc.Add().Serialize((writer, a) => {
                        a.Vector0.Visit(writer, new float[] { -0.9381537f, -0.9835999f, -0.9835999f, -0.9835999f });
                        a.Vector1.Visit(writer, new float[] { -0.8164002f, -0.9836001f, -0.9836001f, -0.9836001f });
                        a.Vector2.Visit(writer, new float[] { 0.01640439f, 0.01640403f, 0.01640403f, 0.01640403f});
                    });


                    polyhedraSc.Add().Serialize((writer, a) => {
                        a.Name.Visit(writer, new StringID(1)); // default
                        a.Radius.Visit(writer, 0.0164f);
                        a.FourVectors.Visit(writer, 2);
                        a.PlaneEquations.Visit(writer, 6);
                        a.VertexCount.Visit(writer, 6);
                        a.AABBHalfExtents.Visit(writer, new float[] { 0.9835999f, 0.08360004f, 0.9836004f, 0f });
                        a.AABBCenter.Visit(writer, new float[] { 0f, -0.9f, 1f, 0f });
                    });

                    planesSc.Add().Serialize((writer, a) => {
                        a.PlaneEquation.Visit(writer, new float[] { 0f, -1f, 0f, -0.9836001f });
                    });
                    planesSc.Add().Serialize((writer, a) => {
                        a.PlaneEquation.Visit(writer, new float[] { 0f, 1f, 0f, 0.8164f });
                    });
                    planesSc.Add().Serialize((writer, a) => {
                        a.PlaneEquation.Visit(writer, new float[] { -0.6944277f, 0.1885228f, 0.6944273f, -0.5089993f });
                    });
                    planesSc.Add().Serialize((writer, a) => {
                        a.PlaneEquation.Visit(writer, new float[] { -0.6826782f, 0.2465233f, 0.6878784f, -0.4505166f });
                    });
                    planesSc.Add().Serialize((writer, a) => {
                        a.PlaneEquation.Visit(writer, new float[] { 1f, 0f, 0f, -0.9835998f });
                    });
                    planesSc.Add().Serialize((writer, a) => {
                        a.PlaneEquation.Visit(writer, new float[] { 0f, 0f, -1f, 0.01640397f });
                    });

                    fourVectorSc.Add().Serialize((writer, a) => {
                        a.Vector0.Visit(writer, new float[] { 0.9835998f, 0.9835998f, 0.9835998f, 0.9835999f });
                        a.Vector1.Visit(writer, new float[] { -0.8164001f, -0.8164001f, -0.9835998f, -0.9835998f });
                        a.Vector2.Visit(writer, new float[] { 1.923637f, 0.01640403f, 1.983605f, 0.01640403f });
                    });

                    fourVectorSc.Add().Serialize((writer, a) => {
                        a.Vector0.Visit(writer, new float[] { -0.9381537f, -0.9835999f, -0.9835999f, -0.9835999f });
                        a.Vector1.Visit(writer, new float[] { -0.8164002f, -0.9836001f, -0.9836001f, -0.9836001f });
                        a.Vector2.Visit(writer, new float[] { 0.01640439f, 0.01640403f, 0.01640403f, 0.01640403f });
                    });

                    polyhedraSc.Add().Serialize((writer, a) => {
                        a.Name.Visit(writer, new StringID(1)); // default
                        a.Radius.Visit(writer, 0.0164f);
                        a.FourVectors.Visit(writer, 2);
                        a.PlaneEquations.Visit(writer, 6);
                        a.VertexCount.Visit(writer, 6);
                        a.AABBHalfExtents.Visit(writer, new float[] { 0.9835999f, 0.08360004f, 0.9836004f, 0f });
                        a.AABBCenter.Visit(writer, new float[] { 0f, -0.9f, 1f, 0f });
                    });

                    planesSc.Add().Serialize((writer, a) => {
                        a.PlaneEquation.Visit(writer, new float[] { 0f, -1f, 0f, -0.9836001f });
                    });
                    planesSc.Add().Serialize((writer, a) => {
                        a.PlaneEquation.Visit(writer, new float[] { 0f, 1f, 0f, 0.8164f });
                    });
                    planesSc.Add().Serialize((writer, a) => {
                        a.PlaneEquation.Visit(writer, new float[] { -0.6944277f, 0.1885228f, 0.6944273f, -0.5089993f });
                    });
                    planesSc.Add().Serialize((writer, a) => {
                        a.PlaneEquation.Visit(writer, new float[] { -0.6826782f, 0.2465233f, 0.6878784f, -0.4505166f });
                    });
                    planesSc.Add().Serialize((writer, a) => {
                        a.PlaneEquation.Visit(writer, new float[] { 1f, 0f, 0f, -0.9835998f });
                    });
                    planesSc.Add().Serialize((writer, a) => {
                        a.PlaneEquation.Visit(writer, new float[] { 0f, 0f, -1f, 0.01640397f });
                    });

                    fourVectorSc.Add().Serialize((writer, a) => {
                        a.Vector0.Visit(writer, new float[] { 0.9835998f, 0.9835998f, 0.9835998f, 0.9835999f });
                        a.Vector1.Visit(writer, new float[] { -0.8164001f, -0.8164001f, -0.9835998f, -0.9835998f });
                        a.Vector2.Visit(writer, new float[] { 1.923637f, 0.01640403f, 1.983605f, 0.01640403f });
                    });

                    fourVectorSc.Add().Serialize((writer, a) => {
                        a.Vector0.Visit(writer, new float[] { -0.9381537f, -0.9835999f, -0.9835999f, -0.9835999f });
                        a.Vector1.Visit(writer, new float[] { -0.8164002f, -0.9836001f, -0.9836001f, -0.9836001f });
                        a.Vector2.Visit(writer, new float[] { 0.01640439f, 0.01640403f, 0.01640403f, 0.01640403f });
                    });

                }

                {
                    var materialSc = sc.GetSerializationContext((phmo) => phmo.MaterialsTagBlock);

                    materialSc.Add().Serialize((writer, a) => {
                        a.Name.Visit(writer, new StringID(1));
                        a.PhantomTypeIndex.Visit(writer, -1);
                    });
                }

                {
                    var nodeSc = sc.GetSerializationContext((phmo) => phmo.NodesTagBlock);

                    nodeSc.Add().Serialize((writer,a) => {
                        a.Name.Visit(writer, new StringID(1));
                        a.ChildIndex.Visit(writer, -1);
                        a.ParentIndex.Visit(writer, -1);
                        a.SiblingIndex.Visit(writer, -1);
                    });
                }

                var otherSC = sc.GetSerializationContext((phmo) => phmo.ListsShapesTagBlock);

                var instance = otherSC.Add();
                instance.Serialize((writer, a) => { a.ShapeIndex.Visit(writer, 2); });


                var blocks = sc.Finish();

                TagContainer container2 = new TagContainer();
                container2.AddTag(new ExtractedTag(new DatumIndex(), 0, CharConstant.FromString("phmo"), "synthesized_tag"));
                foreach(var b in blocks){ container2.AddDataBlock(b); }
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

    public class ContainerBuilder
    {
        protected readonly ICacheContext context;

        protected readonly Dictionary<UInt32, IBlockSerializationContext> blocks;

        protected UInt32 nextMockAddress = 1;

        protected ContainerBuilder(ICacheContext context)
        {
            this.context = context;
            blocks = new Dictionary<uint, IBlockSerializationContext>();
        }

        /// <summary>
        /// A mock address is used to bind a TagRef in an InstanceSerializationContext to a BlockSerializationContext. 
        /// </summary>
        /// <returns></returns>
        public UInt32 GetNextMockAddress()
        {
            return nextMockAddress++;
        }

        protected static RootSerializationContext<T> BuildBase<T>(T schema,
            ICacheContext context) where T : IStructSchema, ITagRoot
        {
            return new BlockSerializationContext<T>(schema, new ContainerBuilder(context), 
                0, (count)=> { }).AddBase<T>();
        }

        public static RootSerializationContext<T> CreateSerializationContext<T>(T schema, 
            ICacheContext context) where T : IStructSchema, ITagRoot
        {
            return BuildBase<T>(schema, context);
        }

        public interface IInstanceSerializationContext
        {
            IReader Reader { get; }
            IWriter Writer { get; }

            IStructSchema Schema { get; }
        }

        public class InstanceSerializationContext<T> : IInstanceSerializationContext where T : IStructSchema
        {
            private readonly T _schema;
            protected ContainerBuilder builder;
            private readonly byte[] _backingData;
            private readonly UInt32 _instanceIndex;

            public InstanceSerializationContext(T schema, ContainerBuilder builder, UInt32 instanceIndex)
            {
                _instanceIndex = instanceIndex;
                _schema = schema;
                _backingData = new byte[schema.Size];
                Reader = new EndianReader(new MemoryStream(_backingData), builder.context.Endian);
                Writer = new EndianWriter(new MemoryStream(_backingData), builder.context.Endian);
            }

            /// <summary>
            /// Creates a Serialization context.
            /// Will create a SC for the relative tagblock.
            /// </summary>
            /// <typeparam name="U">The schema type of the relative tagblock</typeparam>
            /// <param name="action">A path to the tagblock from the schema.</param>
            /// <returns></returns>
            public BlockSerializationContext<U> GetSerializationContext<U>(Func<T,
                ITagBlockRef<U>> action) where U : IStructSchema
            {
                var tagblockRef = action.Invoke(_schema);
                UInt32 mockAddress = tagblockRef.Address.Visit(Reader);
                if (mockAddress != 0)
                {
                    return (BlockSerializationContext<U>)builder.blocks[mockAddress];
                }

                mockAddress = builder.GetNextMockAddress();
                tagblockRef.Address.Visit(Writer, mockAddress);
                var block = new BlockSerializationContext<U>(tagblockRef.Schema, builder,
                    mockAddress, (count) => tagblockRef.Count.Visit(Writer, count));

                return block;
            }

            public void Serialize(Action<IWriter, T> action)
            {
                action.Invoke(Writer, _schema);
                (_schema as IStructWithDataFixup)?.VisitInstance(Writer, _instanceIndex);
            }

            public IReader Reader { get; }
            public IWriter Writer { get; }
            public IStructSchema Schema { get { return _schema; } }
        }

        public interface IBlockSerializationContext
        {
            List<IInstanceSerializationContext> Instances { get; }

            UInt32 Address { get; }
        }

        public class BlockSerializationContext<T> : IBlockSerializationContext where T : IStructSchema
        {
            private readonly T _schema;
            private readonly ContainerBuilder _builder;
            private readonly Action<UInt32> _setParentTagRefCount;

            internal BlockSerializationContext(T schema, ContainerBuilder builder, UInt32 address, Action<UInt32> setParentTagRefCount)
            {
                builder.blocks.Add(address, this);
                Address = address;
                _setParentTagRefCount = setParentTagRefCount;
                _schema = schema;
                _builder = builder;
                Instances = new List<IInstanceSerializationContext>();
            }

            public List<IInstanceSerializationContext> Instances { get; }

            public UInt32 Address { get; }

            public InstanceSerializationContext<T> Add()
            {
                //TODO: also inc ref? or leave this to serialization.
                var instance = new InstanceSerializationContext<T>(_schema, _builder, (UInt32)Instances.Count);
                Instances.Add(instance);
                _setParentTagRefCount((UInt32)Instances.Count);
                return instance;
            }

            public InstanceSerializationContext<T> this[UInt32 i] {
                get => (InstanceSerializationContext<T>)Instances[(int)i];
            }

            /// <summary>
            /// Ignore this.
            /// </summary>
            /// <typeparam name="U"></typeparam>
            /// <returns></returns>
            [Obsolete]
            public RootSerializationContext<U> AddBase<U>() where U : T, ITagRoot
            {
                //TODO: also inc ref? or leave this to serialization.
                var instance = new RootSerializationContext<U>((U)_schema, _builder);
                Instances.Add(instance);
                return instance;
            }
        }

        public class RootSerializationContext<U> : InstanceSerializationContext<U> where U : IStructSchema, ITagRoot
        {
            public RootSerializationContext(U schema, ContainerBuilder builder) : base(schema, builder, 0)
            {
                this.builder = builder;
            }

            public DataBlock[] Finish()
            {
                //TODO:
                var result = new DataBlock[builder.blocks.Count];
                int index = 0;
                foreach (KeyValuePair<UInt32, IBlockSerializationContext> item in builder.blocks) 
                {
                    result[index] = FlattenInstances(0, item.Value.Instances);
                    ++index;
                }
                return result;
            }

        }

        public static DataBlock FlattenInstances(UInt32 originalAddress, List<IInstanceSerializationContext> instances)
        {
            UInt32 alignment = instances[0].Schema.Alignment;
            UInt32 effectiveSize = instances[0].Schema.Size;
            UInt32 paddingBytes = 0;
            if (alignment > 0)
            {
                // Power of 2 check
                if (!((alignment & (alignment - 1)) == 0))
                {
                    throw new InvalidDataException("Alignment for schema " 
                        + instances[0].Schema.GetType().ToString() + " needs to be power of 2.");
                }
                UInt32 mask = alignment - 1;
                paddingBytes = alignment - (effectiveSize & mask);
            }

            byte[] backingData = new byte[instances.Count * (effectiveSize+paddingBytes)];
            var stream = new MemoryStream(backingData);
            for (int i = 0; i < instances.Count; ++i)
            {
                var instanceStream = instances[i].Reader.BaseStream;
                instanceStream.Seek(0, SeekOrigin.Begin);
                instanceStream.CopyTo(stream);
                stream.Seek(paddingBytes, SeekOrigin.Current);
                Console.WriteLine("Position: {0}", stream.Position);
            }

            var result = new DataBlock(originalAddress, instances.Count, (int)alignment, false, backingData);

            return result;
        }

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

    public interface ITagBlockRef
    {
        DataField<UInt32> Count { get; }
        DataField<UInt32> Address { get; }
    }

    public interface ITagBlockRef<SchemaT> : ITagBlockRef
    {
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

    public interface ICacheContext
    {
        Blamite.IO.Endian Endian { get; }
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

    public class MCCReachContext : ICacheContext
    {
        private Dictionary<Type, object> _handlers = new Dictionary<Type, object>()
        {
            { typeof(IPhysicsModelShapeTypes), new PhysicsModelShapeTypes() },
            { typeof(IPhysicsModelMotionTypes), new PhysicsModelMotionTypes() },
        };

        public Blamite.IO.Endian Endian => Endian.LittleEndian;

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

        DataField<Int16> PhantomTypeIndex { get;  }

    }

    class MCCReachPhysicsModelMaterial : IPhysicsModelMaterial
    {
        UInt32 IStructSchema.Size => 0x10;
        UInt32 IStructSchema.Alignment => 4;

        DataField<StringID> IPhysicsModelMaterial.Name => new DataField<StringID>(0);

        DataField<Int16> IPhysicsModelMaterial.PhantomTypeIndex => new DataField<Int16>(0xc);
    }

    interface IPhysicsModelPolyhedra : IStructSchema
    {

        DataField<Blamite.Blam.StringID> Name { get; }
        DataField<byte> PhantomTypeIndex { get; }
        DataField<byte> CollisionGroup { get; }
        DataField<UInt16> Count { get; }
        /// This is a different size on different engines.
        VectorField<byte> InstanceOffset { get; }
        DataField<float> Radius { get;  }
        VectorField<float> AABBHalfExtents { get; }
        VectorField<float> AABBCenter { get; }
        ISizeAndCapacityField FourVectors { get; }
        DataField<Int32> VertexCount { get; }
        ISizeAndCapacityField PlaneEquations { get; }
    }

    class MCCReachPhysicsModelPolyhedra : IPhysicsModelPolyhedra, IStructWithDataFixup
    {
        public UInt32 Size => 0xb0;

        UInt32 IStructSchema.Alignment => 0x10;

        DataField<Blamite.Blam.StringID> IPhysicsModelPolyhedra.Name 
            => new DataField<Blamite.Blam.StringID>(0);
        DataField<byte> IPhysicsModelPolyhedra.PhantomTypeIndex => new DataField<byte>(0x1E);
        DataField<byte> IPhysicsModelPolyhedra.CollisionGroup => new DataField<byte>(0x1F);
        DataField<UInt16> IPhysicsModelPolyhedra.Count => new DataField<UInt16>(0x2A);
        /// <summary>
        /// This is a uint64 (8-bytes) in Halo Reach.
        /// </summary>
        public VectorField<byte> InstanceOffset => new VectorField<byte>(0x30, 8);
        DataField<float> IPhysicsModelPolyhedra.Radius => new DataField<float>(0x40);
        VectorField<float> IPhysicsModelPolyhedra.AABBHalfExtents => new VectorField<float>(0x50, 4);
        VectorField<float> IPhysicsModelPolyhedra.AABBCenter => new VectorField<float>(0x60, 4);
        ISizeAndCapacityField IPhysicsModelPolyhedra.FourVectors 
            => new MCCReachSizeAndCapacityField(0x78);
        DataField<Int32> IPhysicsModelPolyhedra.VertexCount => new DataField<Int32>(0x80);
        ISizeAndCapacityField IPhysicsModelPolyhedra.PlaneEquations 
            => new MCCReachSizeAndCapacityField(0x98);

        

        public void VisitInstance(IWriter writer, uint index)
        {
            writer.SeekTo(this.InstanceOffset[0].Offset);
            writer.WriteUInt64(32 + this.Size*index);
        }
    }

    interface IPhysicsModelLists : IStructSchema
    {
        /// Some odd, poorly-named field. Happens to be '128'. 
        DataField<uint> Count { get; }
        ISizeAndCapacityField ChildShapes { get; }
        VectorField<float> AABBHalfExtents { get; }
        VectorField<float> AABBCenter { get; }
    }

    class MCCReachPhysicsModelLists : IPhysicsModelLists, IStructWithDataFixup
    {
        UInt32 IStructSchema.Size => 0x90;
        UInt32 IStructSchema.Alignment => 0x10;
        public DataField<uint> Count => new DataField<uint>(0xA);
        ISizeAndCapacityField IPhysicsModelLists.ChildShapes => new MCCReachSizeAndCapacityField(0x38);
        VectorField<float> IPhysicsModelLists.AABBHalfExtents => new VectorField<float>(0x50, 4);
        VectorField<float> IPhysicsModelLists.AABBCenter => new VectorField<float>(0x60, 4);

        void IStructWithDataFixup.VisitInstance(IWriter writer, uint index)
        {
            Count.Visit(writer, 128);
        }
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
        UInt32 IStructSchema.Alignment => 0x4;

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
        UInt32 IStructSchema.Alignment => 0x10;
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
        UInt32 IStructSchema.Alignment => 0x10;
        VectorField<float> IPhysicsModelPlaneEquations.PlaneEquation => new VectorField<float>(0, 4);
    }

    interface IPhysicsModelNode : IStructSchema
    {
        DataField<Blamite.Blam.StringID> Name { get; }
        DataField<UInt16> Flags { get; }
        DataField<Int16> ParentIndex { get; }
        DataField<Int16> SiblingIndex { get; }
        DataField<Int16> ChildIndex { get; }
    }

    class PhysicsModelNode : IPhysicsModelNode
    {
        UInt32 IStructSchema.Size => 0xC;
        UInt32 IStructSchema.Alignment => 4;

        DataField<StringID> IPhysicsModelNode.Name => new DataField<StringID>(0);

        DataField<UInt16> IPhysicsModelNode.Flags => new DataField<UInt16>(4);

        DataField<Int16> IPhysicsModelNode.ParentIndex => new DataField<Int16>(6);

        DataField<Int16> IPhysicsModelNode.SiblingIndex => new DataField<Int16>(8);

        DataField<Int16> IPhysicsModelNode.ChildIndex => new DataField<Int16>(10);
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
        UInt32 IStructSchema.Alignment => 4;
        DataField<float> IPhysicsModelRigidBody.BoundingSphereRadius => new DataField<float>(20);
        DataField<byte> IPhysicsModelRigidBody.MotionType => new DataField<byte>(0x1c);
        DataField<UInt16> IPhysicsModelRigidBody.ShapeType => new DataField<UInt16>(168);
        DataField<float> IPhysicsModelRigidBody.Mass => new DataField<float>(0xB0);
        DataField<UInt16> IPhysicsModelRigidBody.ShapeIndex => new DataField<UInt16>(0xAA);
    }

    public interface ITagRoot
    { }

    interface IPhysicsModel : IStructSchema, ITagRoot
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
        UInt32 IStructSchema.Alignment => 4;
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
