using System;
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

        }
    }

    public interface IDataBlock
    {
        UInt32 Size { get; }
    }

    public class DataField<T>
    {
        public DataField(UInt32 offset)
        {
            Offset = offset;
            TypeStub = default(T);
        }
        public UInt32 Offset { protected set; get; }

        public T TypeStub { get;  }
    };

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
        public DataField<uint> Count => new DataField<UInt32>(_offsetInParent + 0);

        public DataField<uint> Address => new DataField<UInt32>(_offsetInParent + 4);

        public T Schema { get; set; }
    }

    interface IPhysicsModelRigidBodyShapeTypes
    {
        // Add more as needed.
        UInt16 List { get;  }
    }

    class MCCReachPhysicsModelRigidBodyShapeTypes : IPhysicsModelRigidBodyShapeTypes
    {
        UInt16 IPhysicsModelRigidBodyShapeTypes.List => 0xe;
    }


    interface IPhysicsModelRigidBody : IDataBlock
    {
        DataField<float> BoundingSphereRadius { get; }
        DataField<UInt16> ShapeTypeOffset { get; }

        IPhysicsModelRigidBodyShapeTypes ShapeTypes { get;  }

    }

    class MCCReachPhysicsModelRigidBody : IPhysicsModelRigidBody
    {
        UInt32 IDataBlock.Size => 208;
        DataField<float> IPhysicsModelRigidBody.BoundingSphereRadius => new DataField<float>(20);
        DataField<UInt16> IPhysicsModelRigidBody.ShapeTypeOffset => new DataField<UInt16>(168);
        IPhysicsModelRigidBodyShapeTypes IPhysicsModelRigidBody.ShapeTypes => new MCCReachPhysicsModelRigidBodyShapeTypes();
    }

    interface IPhysicsModel : IDataBlock
    { 
        ITagBlockRef<IPhysicsModelRigidBody> RigidBodyTagBlock { get; }

    }

    class MCCReachPhysicsModel : IPhysicsModel
    {
        UInt32 IDataBlock.Size => 412;
        ITagBlockRef<IPhysicsModelRigidBody> IPhysicsModel.RigidBodyTagBlock => new MCCReachTagBlockRef<IPhysicsModelRigidBody>(92, new MCCReachPhysicsModelRigidBody());

    }
}
