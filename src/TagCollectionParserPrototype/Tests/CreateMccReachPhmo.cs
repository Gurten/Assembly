/// The Tag Collection Parser Prototype Project
/// Author: Gurten
using Blamite.Blam;
using Blamite.Injection;
using Blamite.IO;
using Blamite.Util;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using TagCollectionParserPrototype.Cache.Core;
using TagCollectionParserPrototype.Cache.MccReach.Context;
using TagCollectionParserPrototype.Cache.Types.Phmo;
using TagCollectionParserPrototype.Schema.MccReach.Phmo;
using TagCollectionParserPrototype.Schema.Phmo;
using TagCollectionParserPrototype.TagSerialization;
using TagCollectionParserPrototype.TagSerialization.ContainerBuilder;

namespace TagCollectionParserPrototype.Tests.CreateMccReachPhmo
{
    class Program
    {
        static void Main2(string[] args)
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
                Utils.WriteToStream(bufferWriter, config.PolyhedraTagBlock.Schema.AABBCenter, new float[] { 0.123f, 0.456f });

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
                        a.AABBHalfExtents.Visit(writer, new float[] { 1f, 1f, 1f, 0.01639998f });
                        a.AABBCenter.Visit(writer, new float[] { 0f, 0f, 1f, 1f });
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
                        a.Xs.Visit(writer, new float[] { 0.9835998f, 0.9835998f, 0.9835998f, 0.9835999f });
                        a.Ys.Visit(writer, new float[] { -0.8164001f, -0.8164001f, -0.9835998f, -0.9835998f });
                        a.Zs.Visit(writer, new float[] { 1.923637f, 0.01640403f, 1.983605f, 0.01640403f });
                    });

                    fourVectorSc.Add().Serialize((writer, a) => {
                        a.Xs.Visit(writer, new float[] { -0.9381537f, -0.9835999f, -0.9835999f, -0.9835999f });
                        a.Ys.Visit(writer, new float[] { -0.8164002f, -0.9836001f, -0.9836001f, -0.9836001f });
                        a.Zs.Visit(writer, new float[] { 0.01640439f, 0.01640403f, 0.01640403f, 0.01640403f });
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
                        a.Xs.Visit(writer, new float[] { 0.9835998f, 0.9835998f, 0.9835998f, 0.9835999f });
                        a.Ys.Visit(writer, new float[] { -0.8164001f, -0.8164001f, -0.9835998f, -0.9835998f });
                        a.Zs.Visit(writer, new float[] { 1.923637f, 0.01640403f, 1.983605f, 0.01640403f });
                    });

                    fourVectorSc.Add().Serialize((writer, a) => {
                        a.Xs.Visit(writer, new float[] { -0.9381537f, -0.9835999f, -0.9835999f, -0.9835999f });
                        a.Ys.Visit(writer, new float[] { -0.8164002f, -0.9836001f, -0.9836001f, -0.9836001f });
                        a.Zs.Visit(writer, new float[] { 0.01640439f, 0.01640403f, 0.01640403f, 0.01640403f });
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
                        a.Xs.Visit(writer, new float[] { 0.9835998f, 0.9835998f, 0.9835998f, 0.9835999f });
                        a.Ys.Visit(writer, new float[] { -0.8164001f, -0.8164001f, -0.9835998f, -0.9835998f });
                        a.Zs.Visit(writer, new float[] { 1.923637f, 0.01640403f, 1.983605f, 0.01640403f });
                    });

                    fourVectorSc.Add().Serialize((writer, a) => {
                        a.Xs.Visit(writer, new float[] { -0.9381537f, -0.9835999f, -0.9835999f, -0.9835999f });
                        a.Ys.Visit(writer, new float[] { -0.8164002f, -0.9836001f, -0.9836001f, -0.9836001f });
                        a.Zs.Visit(writer, new float[] { 0.01640439f, 0.01640403f, 0.01640403f, 0.01640403f });
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

                    nodeSc.Add().Serialize((writer, a) => {
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
                container2.AddTag(new ExtractedTag(new DatumIndex(), 0, CharConstant.FromString("phmo"), "synthesized_tag5"));
                foreach (var b in blocks) { container2.AddDataBlock(b); }

                string outputPath = @"C:\Users\gurten\Documents\tags\reach\synthesized.tagc";
                using (var writer = new EndianWriter(File.Open(outputPath, FileMode.Create, FileAccess.Write), context.Endian))
                {
                    TagContainerWriter.WriteTagContainer(container2, writer);
                }


            }
        }
    }
}
