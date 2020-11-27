/// The Tag Collection Parser Prototype Project
/// Author: Gurten
using Blamite.Injection;
using Blamite.IO;
using System;
using System.Collections.Generic;
using System.IO;
using TagCollectionParserPrototype.Cache.Core;
using TagCollectionParserPrototype.Schema.Core;


namespace TagCollectionParserPrototype.TagSerialization.ContainerBuilder
{
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
                0, (count) => { }).AddBase<T>();
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

            List<DataBlockAddressFixup> AddressFixups { get; }

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
                this.builder = builder;
                _backingData = new byte[schema.Size];
                Reader = new EndianReader(new MemoryStream(_backingData, 0, _backingData.Length, false), builder.context.Endian);
                Writer = new EndianWriter(new MemoryStream(_backingData, 0, _backingData.Length, true), builder.context.Endian);
                AddressFixups = new List<DataBlockAddressFixup>();
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
                AddressFixups.Add(new DataBlockAddressFixup(mockAddress,
                    (int)(_instanceIndex * Utils.GetAlignedSize(_schema) + tagblockRef.Address.Offset)));
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

            public List<DataBlockAddressFixup> AddressFixups { get; }
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
                var instance = new InstanceSerializationContext<T>(_schema, _builder, (UInt32)Instances.Count);
                Instances.Add(instance);
                _setParentTagRefCount((UInt32)Instances.Count);
                return instance;
            }

            public InstanceSerializationContext<T> this[UInt32 i]
            {
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

        public interface IRootSerializationContext
        {
            DataBlock[] Finish();
        }

        public class RootSerializationContext<U> : InstanceSerializationContext<U>, 
            IRootSerializationContext where U : IStructSchema, ITagRoot
        {
            public RootSerializationContext(U schema, ContainerBuilder builder) : base(schema, builder, 0)
            {
                this.builder = builder;
            }

            public DataBlock[] Finish()
            {
                var result = new List<DataBlock>(builder.blocks.Count);
                foreach (KeyValuePair<UInt32, IBlockSerializationContext> item in builder.blocks)
                {
                    if (item.Value.Instances.Count <= 0)
                    {
                        continue;
                    }
                    result.Add(builder.FlattenInstances(item.Key, item.Value.Instances));
                }
                return result.ToArray();
            }

        }

        public DataBlock FlattenInstances(UInt32 originalAddress,
            List<IInstanceSerializationContext> instances)
        {
            UInt32 alignedSize = Utils.GetAlignedSize(instances[0].Schema);
            UInt32 paddingBytes = alignedSize - instances[0].Schema.Size;

            byte[] backingData = new byte[instances.Count * alignedSize];
            var stream = new MemoryStream(backingData);
            List<DataBlockAddressFixup> allAddressFixupsFromInstances
                = new List<DataBlockAddressFixup>();
            for (int i = 0; i < instances.Count; ++i)
            {
                var instanceStream = instances[i].Reader.BaseStream;
                instanceStream.Seek(0, SeekOrigin.Begin);
                instanceStream.CopyTo(stream);
                stream.Seek(paddingBytes, SeekOrigin.Current);
                Console.WriteLine("Position: {0}", stream.Position);
                foreach (var fixup in instances[i].AddressFixups)
                { allAddressFixupsFromInstances.Add(fixup); }
            }

            var result = new DataBlock(originalAddress, instances.Count,
                (int)instances[0].Schema.Alignment, false, backingData);
            result.AddressFixups.Capacity = allAddressFixupsFromInstances.Count;
            foreach (var fixup in allAddressFixupsFromInstances)
            {
                if (!blocks.ContainsKey(fixup.OriginalAddress) 
                    || blocks[fixup.OriginalAddress].Instances.Count <= 0)
                {
                    // Detects references to non-existent tagblocks. This could happen if 
                    // if you create a serialization-context, but end up not adding any 
                    // instances.
                    continue;
                }
                result.AddressFixups.Add(fixup); 
            }

            return result;
        }

    }
}
