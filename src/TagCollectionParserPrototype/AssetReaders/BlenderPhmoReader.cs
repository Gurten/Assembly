/// The Tag Collection Parser Prototype Project
/// Author: Gurten
using System;
using System.IO;
using Blamite.Blam;
using Blamite.Injection;
using Blamite.IO;
using Blamite.Util;
using NUnit.Framework;
using SimpleJSON;
using TagCollectionParserPrototype.Cache.Core;
using TagCollectionParserPrototype.Cache.MccReach.Context;
using TagCollectionParserPrototype.Cache.Types.Phmo;
using TagCollectionParserPrototype.Schema.Phmo;
using TagCollectionParserPrototype.TagSerialization;
using TagCollectionParserPrototype.TagSerialization.ContainerBuilder;

namespace AssetReaders.Geometry
{
    /// <summary>
    /// This class loads, reads, tokenises, and parses a simple file format
    /// designed to store data exported from the Blender modeling program. 
    /// </summary>
    class BlenderPhmoReader
    {

        public string filename;

        public BlenderPhmoReader(string fname)
        {
            filename = fname;
        }

        public JSONNode ReadFile()
        {
            string contents;
            try
            {
                // open the file as a text-stream
                StreamReader sr = new StreamReader(filename);
                contents = sr.ReadToEnd();
                sr.Close();
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("File: {0} could not be found.", filename);
                return null;
            };

            //parse the file as json
            var json = JSON.Parse(contents);

            return json;
        }
        static void PrintUsage()
        {
            Console.WriteLine("program <halo3|reach> <path to json> <output path>");
        }
        static void Create(){ //...main
            IPhysicsModel config = null;
            ICacheContext context = new MCCReachContext();
            string jsonPath = "";
            BlenderPhmoReader reader = new BlenderPhmoReader(jsonPath);
            var jsonFileRoot = reader.ReadFile();
            if (jsonFileRoot == null)
            {
                PrintUsage();
                return;
            }
            Assert.IsNotNull(jsonFileRoot, "Could not parse json file.");
            var shapeDefinitions = jsonFileRoot.AsArray;

            int jsonShapeCount = shapeDefinitions.Count;

            Assert.Greater(jsonShapeCount, 0, "No shapes in json.");

            var sc = ContainerBuilder.CreateSerializationContext(config, context);

            var materialSc = sc.GetSerializationContext((phmo) => phmo.MaterialsTagBlock);
            materialSc.Add().Serialize((writer, a) => {
                a.Name.Visit(writer, new StringID(1));
                a.PhantomTypeIndex.Visit(writer, -1);
            });

            var nodeSc = sc.GetSerializationContext((phmo) => phmo.NodesTagBlock);
            nodeSc.Add().Serialize((writer, a) => {
                a.Name.Visit(writer, new StringID(1));
                a.ChildIndex.Visit(writer, -1);
                a.ParentIndex.Visit(writer, -1);
                a.SiblingIndex.Visit(writer, -1);
            });

            var rigidBodySc = sc.GetSerializationContext((phmo) => phmo.RigidBodyTagBlock);
            rigidBodySc.Add().Serialize((writer, a) =>
            {
                a.BoundingSphereRadius.Visit(writer, 123f);
                a.MotionType.Visit(writer, context.Get<IPhysicsModelMotionTypes>().Keyframed);
                a.Mass.Visit(writer, 100f);
                a.ShapeIndex.Visit(writer, 0);

                var runtimeFlags = context.Get<IPhysicsModelRigidBodyRuntimeFlags>();
                // Makes things collidable!
                a.RuntimeFlags.Visit(writer, runtimeFlags.SuperImportantFlag);

                var shapeTypeOptions = context.Get<IPhysicsModelShapeTypes>();
                a.ShapeType.Visit(writer, jsonShapeCount > 1 ?
                    shapeTypeOptions.List : shapeTypeOptions.Polyhedron);

            });

            if (jsonShapeCount > 1)
            {
                //More than one shape, need to make a list-shape to hold them all.
                var listShapesSc = sc.GetSerializationContext((phmo) => phmo.ListsShapesTagBlock);
                UInt16 amountAdded = 0;
                foreach (JSONNode shapeNode in shapeDefinitions)
                {
                    bool success = AddShape(sc, context, shapeNode);
                    Assert.IsTrue(success, "could not add shape: " + shapeNode.ToString());


                    listShapesSc.Add().Serialize((writer, a) => {
                        a.ShapeIndex.Visit(writer, amountAdded);
                        a.ChildShapeCount.Visit(writer, (UInt16)jsonShapeCount);
                        a.ShapeType.Visit(writer, context.Get<IPhysicsModelShapeTypes>().Polyhedron);
                    });

                    ++amountAdded;
                }

                var listSc = sc.GetSerializationContext((phmo) => phmo.ListsTagBlock);
                listSc.Add().Serialize((writer, a) => {
                    //a.AABBHalfExtents.Visit(writer, new float[] { 1f, 1f, 1f, 0.01639998f });
                    //a.AABBCenter.Visit(writer, new float[] { 0f, 0f, 1f, 1f });
                    a.ChildShapes.Visit(writer, (UInt32)jsonShapeCount); // put the actual value here.
                });

            }
            else
            {
                bool success = AddShape(sc, context, shapeDefinitions[0]);
            }

            var blocks = sc.Finish();

            string outputPath = "";
            string tagName = outputPath;
            outputPath = outputPath.Replace("/", "\\");
            int lastPathSepIndex = outputPath.LastIndexOf("\\");
            if (lastPathSepIndex > 0)
            {
                Directory.CreateDirectory(outputPath.Substring(0, lastPathSepIndex));
                tagName = outputPath.Substring(lastPathSepIndex+1);
            }

            tagName = tagName.Replace(".", "_");
            TagContainer container = new TagContainer();
            container.AddTag(new ExtractedTag(new DatumIndex(), 0, CharConstant.FromString("phmo"), tagName));
            foreach (var b in blocks) { container.AddDataBlock(b); }
            
            using (var writer = new EndianWriter(File.Open(outputPath, FileMode.Create, FileAccess.Write), context.Endian))
            {
                TagContainerWriter.WriteTagContainer(container, writer);
            }
            
        }
        static bool AddShape(ContainerBuilder.RootSerializationContext<IPhysicsModel> sc, ICacheContext context, JSONNode node)
        {
            Assert.IsNotNull(node, "Could not parse shape");
            switch (node["Type"])
            {
                case "Polyhedron":
                    return AddPolyhedron(sc, context, node["Data"]);
                default:
                    return false;
            }
        }

        static bool AddPolyhedron(ContainerBuilder.RootSerializationContext<IPhysicsModel> sc, ICacheContext context, JSONNode node)
        {
            Assert.IsNotNull(node, "Could not parse shape");
            float friction = node["Friction"].AsFloat;
            float mass = node["Mass"].AsFloat;
            float restitution = node["Restitution"].AsFloat;
            var center = node["Center"].AsArray;
            var extents = node["Extents"].AsArray;

            int nPlanes = AddManyPlanes(sc, context, node["Planes"]);
            Assert.Greater(nPlanes, 0, "No planes in polyhedron definition. This is invalid.");

            int nFourVectors = AddManyFourVectors(sc, context, node["Vertices"]);
            Assert.Greater(nFourVectors, 0, "No vertices in polyhedron definition. This is invalid.");

            var polyhedraSc = sc.GetSerializationContext((phmo) => phmo.PolyhedraTagBlock);
            polyhedraSc.Add().Serialize((writer, a) => {
                a.Name.Visit(writer, new StringID(1)); // default
                //a.Radius.Visit(writer, 0.0164f);

                a.FourVectors.Visit(writer, (UInt32)nFourVectors);
                a.PlaneEquations.Visit(writer, (UInt32)nPlanes);
                //a.VertexCount.Visit(writer, 6);

                a.AABBHalfExtents.Visit(writer, new float[] { extents[0].AsFloat, extents[1].AsFloat, extents[2].AsFloat, 0f });
                a.AABBCenter.Visit(writer, new float[] { center[0].AsFloat, center[1].AsFloat, center[2].AsFloat, 0f });

                //TODO: add to context
                a.PhantomTypeIndex.Visit(writer, 255);
            });
            return true;
        }

        static int AddManyPlanes(ContainerBuilder.RootSerializationContext<IPhysicsModel> sc, ICacheContext context, JSONNode node)
        {
            Assert.IsNotNull(node, "Could not parse polyhedron's planes.");
            var planesSc = sc.GetSerializationContext((phmo) => phmo.PolyhedraPlaneEquationsTagBlock);

            var planeDefinitions = node.AsArray;
            foreach (JSONNode p in planeDefinitions)
            {
                var planeEquation = p.AsArray;
                planesSc.Add().Serialize((writer, a) => {
                    a.PlaneEquation.Visit(writer, new float[] { 
                        planeEquation[0].AsFloat, 
                        planeEquation[1].AsFloat, 
                        planeEquation[2].AsFloat, 
                        planeEquation[3].AsFloat 
                    });
                });
            }

            return planeDefinitions.Count;
        }

        static int AddManyFourVectors(ContainerBuilder.RootSerializationContext<IPhysicsModel> sc, ICacheContext context, JSONNode node)
        {
            Assert.IsNotNull(node, "Could not parse polyhedron's vertices.");

            var fourVectorSc = sc.GetSerializationContext((phmo) => phmo.PolyhedraFourVectorTagBlock);

            var vertices = node.AsArray;
            int vertexCount = vertices.Count;
            UInt32 fourVectorCount = Utils.GetAlignedSize((UInt32)vertexCount, 4)/4;
            int fullyUsedFourVectorCount = vertices.Count / 4;
            for (int i = 0; i < fullyUsedFourVectorCount; ++i)
            {
                JSONNode v0 = vertices[i * 4];
                JSONNode v1 = vertices[i * 4 + 1];
                JSONNode v2 = vertices[i * 4 + 2];
                JSONNode v3 = vertices[i * 4 + 3];

                fourVectorSc.Add().Serialize((writer, a) => {
                    a.Xs.Visit(writer, new float[] { v0[0].AsFloat, v1[0].AsFloat, v2[0].AsFloat, v3[0].AsFloat });
                    a.Ys.Visit(writer, new float[] { v0[1].AsFloat, v1[1].AsFloat, v2[1].AsFloat, v3[1].AsFloat });
                    a.Zs.Visit(writer, new float[] { v0[2].AsFloat, v1[2].AsFloat, v2[2].AsFloat, v3[2].AsFloat });
                });
            }

            if (fullyUsedFourVectorCount != fourVectorCount)
            {
                // Add leftovers, pad with the very last vertex to the next multiple of four-vertices.
                int lastRowBegin = fullyUsedFourVectorCount * 4;
                JSONNode v0 = vertices[Math.Min(vertexCount-1, lastRowBegin)];
                JSONNode v1 = vertices[Math.Min(vertexCount-1, lastRowBegin+1)];
                JSONNode v2 = vertices[Math.Min(vertexCount-1, lastRowBegin+2)];
                JSONNode v3 = vertices[Math.Min(vertexCount-1, lastRowBegin+3)];
                fourVectorSc.Add().Serialize((writer, a) => {
                    a.Xs.Visit(writer, new float[] { v0[0].AsFloat, v1[0].AsFloat, v2[0].AsFloat, v3[0].AsFloat });
                    a.Ys.Visit(writer, new float[] { v0[1].AsFloat, v1[1].AsFloat, v2[1].AsFloat, v3[1].AsFloat });
                    a.Zs.Visit(writer, new float[] { v0[2].AsFloat, v1[2].AsFloat, v2[2].AsFloat, v3[2].AsFloat });
                });
            }

            return (int)fourVectorCount;
        }
         

    }
}
