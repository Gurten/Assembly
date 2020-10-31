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
            IPhysicsModelConfig config = new MCCReachPhysicsModelConfig();
            string tagFilePath = @"C:\Users\gurten\Documents\tags\reach\ff_plat_1x1.tagc";
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
            Assert.AreEqual(config.RootTagblockSize, tagData.EntrySize);

            var rigidBodyDataBlockFixup = tagData.AddressFixups.ElementAt(0);
            Assert.AreEqual(config.RootRigidBodyTagBlockOffset + config.TagblockRefAddressOffset,
                rigidBodyDataBlockFixup.WriteOffset);

            var rigidBodyDataBlock = container.FindDataBlock(rigidBodyDataBlockFixup.OriginalAddress);
            Assert.IsNotNull(rigidBodyDataBlock, "could not find rigidbody tagblock");

            Assert.AreEqual(1, rigidBodyDataBlock.EntryCount);
            Assert.AreEqual(config.RigidBodyDataBlockSize, rigidBodyDataBlock.EntrySize);

        }
    }



    interface IPhysicsModelConfig
    { 
        UInt32 RootTagblockSize { get; }

        //The offset within a tagblock reference for the address where the tagblock begins.
        UInt32 TagblockRefAddressOffset { get; }
        UInt32 RootRigidBodyTagBlockOffset { get; }

        UInt32 RigidBodyDataBlockSize { get; }
    }

    class MCCReachPhysicsModelConfig : IPhysicsModelConfig
    {
        UInt32 IPhysicsModelConfig.RootRigidBodyTagBlockOffset => 92;

        UInt32 IPhysicsModelConfig.RootTagblockSize => 412;

        UInt32 IPhysicsModelConfig.TagblockRefAddressOffset => 4;

        UInt32 IPhysicsModelConfig.RigidBodyDataBlockSize => 208;
    }
}
