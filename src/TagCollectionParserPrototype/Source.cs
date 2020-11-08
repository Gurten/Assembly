﻿/// The Tag Collection Parser Prototype Project
/// Author: Gurten
using System;
using System.IO;
using Blamite.Blam;
using Blamite.Injection;
using Blamite.IO;
using Blamite.Util;
using TagCollectionParserPrototype.Cache.Core;
using TagCollectionParserPrototype.Cache.MccReach.Context;
using TagCollectionParserPrototype.Cache.Types.Phmo;
using TagCollectionParserPrototype.Schema.MccReach.Phmo;
using TagCollectionParserPrototype.Schema.Phmo;
using TagCollectionParserPrototype.TagSerialization.ContainerBuilder;

//using SimpleJSON;

namespace TagCollectionParserPrototype
{
    class Program
    {

        static void Main(string[] args)
        {
            IPhysicsModel config = new MCCReachPhysicsModel();
            ICacheContext context = new MCCReachContext();
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
