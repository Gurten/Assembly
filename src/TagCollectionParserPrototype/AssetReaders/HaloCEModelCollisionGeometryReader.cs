/// The Tag Collection Parser Prototype Project
/// Author: Gurten
using System;
using System.IO;
using System.Linq;
using Blamite.Blam;
using Blamite.Injection;
using TagCollectionParserPrototype.Cache.Core;
using TagCollectionParserPrototype.Schema.Coll;
using TagCollectionParserPrototype.Schema.Coll.region;
using TagCollectionParserPrototype.Schema.Coll.region.permutation;
using TagCollectionParserPrototype.TagSerialization.ContainerBuilder;

namespace AssetReaders.Geometry
{
    class CollisionGeometryBuilder
    {
        /// <summary>
        /// Offset and Size values for H1CE tagblock structs
        /// </summary>
        const int
            MAIN_STRUCT_OFFSET = 64,
            MAIN_STRUCT_SIZE = 664,
            MAIN_MATERIAL_OFFSET = 564,
            MAIN_REGION_OFFSET = 576,
            MAIN_PATHF_SPHERES_OFFSET = 640,
            MAIN_NODES_OFFSET = 652,
            MATERIAL_TAGBLOCK_SIZE = 72,
            REGION_TAGBLOCK_SIZE = 84,
            REGION_PERMUTATION_OFFSET = 72,
            PERMUTATION_SIZE = 32,
            PATHF_SPHERE_SIZE = 32,
            NODE_SIZE = 64,
            BSP_BSP3DNODES_OFFSET = 0,
            BSP_PLANES_OFFSET = 12,
            BSP_LEAVES_OFFSET = 24,
            BSP_BSP2DREFERENCES_OFFSET = 36,
            BSP_BSP2DNODES_OFFSET = 48,
            BSP_SURFACES_OFFSET = 60,
            BSP_EDGES_OFFSET = 72,
            BSP_VERTICES_OFFSET = 84,
            BSP_SIZE = 96,
            BSP3DNODE_SIZE = 12,
            PLANE_SIZE = 16,
            LEAF_SIZE = 8,
            BSP2DREFERENCE_SIZE = 8,
            BSP2DNODE_SIZE = 20,
            SURFACE_SIZE = 12,
            EDGE_SIZE = 24,
            VERTEX_SIZE = 16;

        protected ContainerBuilder.IRootSerializationContext RootSerializationContext;

        public CollisionGeometryBuilder() { }

        /// <summary>
        /// Stub for now, creates n materials, which are named 0 to (n-1)
        /// </summary>
        /// <param name="coll"></param>
        /// <param name="tag_data"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        protected long ParseMaterials(ContainerBuilder.RootSerializationContext<ICollisionModel> coll, BinaryReader reader, int count)
        {
            var materials = coll.GetSerializationContext((coll2) => coll2.Materials);

            for (uint i = 0; i < count; ++i)
            {
                materials.Add().Serialize((writer, a) => {
                    a.Name.Visit(writer, new StringID(0x140 + i));
                });
            }

            return reader.BaseStream.Position + MATERIAL_TAGBLOCK_SIZE * count;
        }

        /// <summary>
        /// Parses regions into Halo Online collision 'Region' tagblocks.
        /// Names are not preserved.
        /// </summary>
        /// <param name="coll"></param>
        /// <param name="reader"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        protected long ParseRegions(
            ContainerBuilder.RootSerializationContext<ICollisionModel> coll,
            BinaryReader reader, int count)
        {
            var originalPos = reader.BaseStream.Position;

            //The number of permutations that have been parsed in total for all regions
            var n_parsed_permutations = 0;
            //Permutations for all regions follow sequentially after all regions
            var permutations_base_addr = reader.BaseStream.Position
                + count * REGION_TAGBLOCK_SIZE;

            //var regions = coll.GetSerializationContext((coll2) => coll2.Regions);

            for (uint i = 0; i < count; ++i)
            {
                //configure the current region
                //var region = regions.Add();
                //var regionPermutations = region.GetSerializationContext((region2) => region2.Permutations);
                // region.Serialize((writer, a) => {
                //     a.Name.Visit(writer, new StringID(0x140 + i));

                //set up stream for reading number of permutations in current region
                reader.BaseStream.Position = originalPos
                    + (long)(i * REGION_TAGBLOCK_SIZE)
                    + REGION_PERMUTATION_OFFSET;

                uint n_permutations = (uint)BitConverter.ToInt32(reader.ReadBytes(4).Reverse().ToArray(), 0);
                for (uint j = 0; j < n_permutations; ++j)
                {
                    //regionPermutations.Add().Serialize((writer2, b) => {
                    //    b.Name.Visit(writer2, new StringID(0x140+j));
                    //});
                }
                n_parsed_permutations += (int)n_permutations;
                //});

            }

            return permutations_base_addr + (n_parsed_permutations * PERMUTATION_SIZE);
        }

        protected long ParsePathFindingSpheres(
            ContainerBuilder.RootSerializationContext<ICollisionModel> coll,
            BinaryReader reader, int count)
        {
            var originalPos = reader.BaseStream.Position;

            var pathfindingSpheres = coll.GetSerializationContext((coll2) => coll2.PathFindingSpheres);

            for (uint i = 0; i < count; ++i)
            {
                reader.BaseStream.Position = originalPos + (i * PATHF_SPHERE_SIZE);
                var node_idx = BitConverter.ToInt16(reader.ReadBytes(2).Reverse().ToArray(), 0);
                reader.BaseStream.Position += 14; //14 bytes between node index and sphere location,radius
                var center_x = BitConverter.ToSingle(reader.ReadBytes(4).Reverse().ToArray(), 0);
                var center_y = BitConverter.ToSingle(reader.ReadBytes(4).Reverse().ToArray(), 0);
                var center_z = BitConverter.ToSingle(reader.ReadBytes(4).Reverse().ToArray(), 0);
                var radius = BitConverter.ToSingle(reader.ReadBytes(4).Reverse().ToArray(), 0);

                pathfindingSpheres.Add().Serialize((writer, a) =>
                {
                    a.NodeIndex.Visit(writer, node_idx);
                    a.SphereLocation.Visit(writer, new float[] { center_x, center_y, center_z });
                    a.SphereRadius.Visit(writer, radius);
                });
            }

            return originalPos + (count * PATHF_SPHERE_SIZE);
        }

        /// <summary>
        /// Parses all H1CE Collision Node tagblocks stored sequentially.
        /// The purpose of 'Node' is similar to 'Region' in Halo Online.
        /// </summary>
        /// <param name="coll"></param>
        /// <param name="reader"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        protected long ParseNodes(
            ContainerBuilder.RootSerializationContext<ICollisionModel> coll,
            BinaryReader reader, int count)
        {
            var originalPos = reader.BaseStream.Position;
            var nodes = coll.GetSerializationContext((coll2) => coll2.Nodes);
            //destroy the old list of regions, it may not be fine-grained enough
            var regions = coll.GetSerializationContext((coll2) => coll2.Regions);

            var new_region_count = 0;
            var current_bsp_offset = originalPos + (count * NODE_SIZE);

            for (uint i = 0; i < count; ++i)
            {
                //offset of the parent node in the h1 ce node tagblock
                reader.BaseStream.Position = originalPos + (i * NODE_SIZE) + 32;
                short region_idx = BitConverter.ToInt16(reader.ReadBytes(2).Reverse().ToArray(), 0);
                short parent_node_idx = BitConverter.ToInt16(reader.ReadBytes(2).Reverse().ToArray(), 0);
                short next_sibling_idx = BitConverter.ToInt16(reader.ReadBytes(2).Reverse().ToArray(), 0);
                short first_child_idx = BitConverter.ToInt16(reader.ReadBytes(2).Reverse().ToArray(), 0);

                var node = nodes.Add();
                node.Serialize((writer, a) => {
                    a.Name.Visit(writer, new StringID(0x140 + i));
                    a.ParentNodeIndex.Visit(writer, parent_node_idx);
                    a.NextSiblingNodeIndex.Visit(writer, next_sibling_idx);
                    a.FirstChildNodeIndex.Visit(writer, first_child_idx);
                });

                //there exists a region when the region index of the h1 ce collision node tagblock is not null
                if (region_idx >= 0)
                {
                    var region = regions.Add();

                    region.Serialize((writer, a) => {
                        a.Name.Visit(writer, new StringID(0x140 + (uint)new_region_count));
                        reader.BaseStream.Position = originalPos + (i * NODE_SIZE) + 52; //bsp tagblock count

                        var permutations = region.GetSerializationContext((region2) => region2.Permutations);
                        var permutation = permutations.Add();
                        permutation.Serialize((writer2, b) => {
                            b.Name.Visit(writer, new StringID(0x140));
                        });
                        //var bsps = permutation.GetSerializationContext((permutation2) => permutation2.BSPs);

                        uint n_bsps = (uint)BitConverter.ToInt32(reader.ReadBytes(4).Reverse().ToArray(), 0);
                        for (uint j = 0; j < n_bsps; ++j)
                        {
                            reader.BaseStream.Position = current_bsp_offset;
                            current_bsp_offset = ParseBSP(permutation, reader);
                        }

                    });

                    new_region_count++;
                }
            }

            return current_bsp_offset;
        }

        protected long ParseBSP(
            ContainerBuilder.InstanceSerializationContext<IPermutation> permutation,
            BinaryReader reader)
        {
            long originalPos = reader.BaseStream.Position;
            var bsp = permutation.GetSerializationContext((p) => p.BSPs).Add();

            reader.BaseStream.Position = originalPos + BSP_BSP3DNODES_OFFSET;
            int n_3dnodes = BitConverter.ToInt32(reader.ReadBytes(4).Reverse().ToArray(), 0);

            reader.BaseStream.Position = originalPos + BSP_PLANES_OFFSET;
            int n_planes = BitConverter.ToInt32(reader.ReadBytes(4).Reverse().ToArray(), 0);

            reader.BaseStream.Position = originalPos + BSP_LEAVES_OFFSET;
            int n_leaves = BitConverter.ToInt32(reader.ReadBytes(4).Reverse().ToArray(), 0);

            reader.BaseStream.Position = originalPos + BSP_BSP2DREFERENCES_OFFSET;
            int n_2dreferences = BitConverter.ToInt32(reader.ReadBytes(4).Reverse().ToArray(), 0);

            reader.BaseStream.Position = originalPos + BSP_BSP2DNODES_OFFSET;
            int n_2dnodes = BitConverter.ToInt32(reader.ReadBytes(4).Reverse().ToArray(), 0);

            reader.BaseStream.Position = originalPos + BSP_SURFACES_OFFSET;
            int n_surfaces = BitConverter.ToInt32(reader.ReadBytes(4).Reverse().ToArray(), 0);

            reader.BaseStream.Position = originalPos + BSP_EDGES_OFFSET;
            int n_edges = BitConverter.ToInt32(reader.ReadBytes(4).Reverse().ToArray(), 0);

            reader.BaseStream.Position = originalPos + BSP_VERTICES_OFFSET;
            int n_vertices = BitConverter.ToInt32(reader.ReadBytes(4).Reverse().ToArray(), 0);

            reader.BaseStream.Position = originalPos + BSP_SIZE;
            reader.BaseStream.Position = ParseBSP3DNodes(bsp, reader, n_3dnodes);
            reader.BaseStream.Position = ParsePlanes(bsp, reader, n_planes);
            reader.BaseStream.Position = ParseLeaves(bsp, reader, n_leaves);
            reader.BaseStream.Position = ParseBSP2DReferences(bsp, reader, n_2dreferences);
            reader.BaseStream.Position = ParseBSP2DNodes(bsp, reader, n_2dnodes);
            reader.BaseStream.Position = ParseSurfaces(bsp, reader, n_surfaces);
            reader.BaseStream.Position = ParseEdges(bsp, reader, n_edges);
            return ParseVertices(bsp, reader, n_vertices);
        }

        protected long ParseBSP3DNodes(
            ContainerBuilder.InstanceSerializationContext<IBSP> bsp,
            BinaryReader reader, int count)
        {
            var bsp3dNodes = bsp.GetSerializationContext((bsp2) => bsp2.BSP3DNodes);
            long originalPos = reader.BaseStream.Position;
            for (uint i = 0; i < count; ++i)
            {
                reader.BaseStream.Position = originalPos + (i * BSP3DNODE_SIZE);
                var plane_idx = BitConverter.ToInt32(reader.ReadBytes(4).Reverse().ToArray(), 0);
                var back_child = BitConverter.ToInt32(reader.ReadBytes(4).Reverse().ToArray(), 0);
                var front_child = BitConverter.ToInt32(reader.ReadBytes(4).Reverse().ToArray(), 0);

                //compress back and front children to int24.

                //remove bits 24 and above
                var back_child_trun = back_child & 0x7fffff;
                var front_child_trun = front_child & 0x7fffff;

                //add the new signs
                if (back_child < 0)
                    back_child_trun |= 0x800000;
                if (front_child < 0)
                    front_child_trun |= 0x800000;

                //truncate the plane index with control over the result
                int uplane_idx = (plane_idx & 0x7fff);
                if (plane_idx < 0)
                    uplane_idx |= 0x8000;


                var bsp3dNode = bsp3dNodes.Add();
                bsp3dNode.Serialize((writer, a) => {
                    //perhaps put message here to notify that plane index was out of the range
                    a.PlaneIndex.Visit(writer, (short)uplane_idx);
                    if (writer.Endianness == Blamite.IO.Endian.LittleEndian)
                    {
                        a.BackChild.Visit(writer, new[] {
                            (byte)(back_child_trun & 0xff),
                            (byte)((back_child_trun >> 8) & 0xff),
                            (byte)((back_child_trun >> 16) & 0xff)
                        });

                        a.FrontChild.Visit(writer, new[] {
                            (byte)(front_child_trun & 0xff),
                            (byte)((front_child_trun >> 8) & 0xff),
                            (byte)((front_child_trun >> 16) & 0xff)
                        });
                    }
                    else
                    {
                        a.BackChild.Visit(writer, new[] {
                            (byte)((back_child_trun >> 16) & 0xff),
                            (byte)((back_child_trun >> 8) & 0xff),
                            (byte)(back_child_trun & 0xff)
                        });

                        a.FrontChild.Visit(writer, new[] {
                            (byte)((front_child_trun >> 16) & 0xff),
                            (byte)((front_child_trun >> 8) & 0xff),
                            (byte)(front_child_trun & 0xff)
                        });
                    }

                });
            }

            return originalPos + (count * BSP3DNODE_SIZE);
        }

        protected long ParsePlanes(
            ContainerBuilder.InstanceSerializationContext<IBSP> bsp,
            BinaryReader reader, int count)
        {
            var planes = bsp.GetSerializationContext((bsp2) => bsp2.Planes);
            var originalPos = reader.BaseStream.Position;

            for (uint i = 0; i < count; ++i)
            {
                reader.BaseStream.Position = originalPos + (i * PLANE_SIZE);
                var plane_i = BitConverter.ToSingle(reader.ReadBytes(4).Reverse().ToArray(), 0);
                var plane_j = BitConverter.ToSingle(reader.ReadBytes(4).Reverse().ToArray(), 0);
                var plane_k = BitConverter.ToSingle(reader.ReadBytes(4).Reverse().ToArray(), 0);
                var plane_d = BitConverter.ToSingle(reader.ReadBytes(4).Reverse().ToArray(), 0);

                planes.Add().Serialize((writer, a) => {
                    a.EquationIJKD.Visit(writer, new float[] {
                    plane_i, plane_j, plane_k, plane_d
                    });
                });
            }

            return originalPos + (count * PLANE_SIZE);
        }

        protected long ParseLeaves(
            ContainerBuilder.InstanceSerializationContext<IBSP> bsp,
            BinaryReader reader, int count)
        {
            var leaves = bsp.GetSerializationContext((bsp2) => bsp2.Leaves);
            var originalPos = reader.BaseStream.Position;

            for (uint i = 0; i < count; ++i)
            {
                reader.BaseStream.Position = originalPos + (i * LEAF_SIZE);
                var flags = BitConverter.ToInt16(reader.ReadBytes(2).Reverse().ToArray(), 0);
                var bsp2dRefCount = BitConverter.ToUInt16(reader.ReadBytes(2).Reverse().ToArray(), 0);
                var Unknown = BitConverter.ToInt32(reader.ReadBytes(4).Reverse().ToArray(), 0);
                var first2dRef = BitConverter.ToInt16(reader.ReadBytes(2).Reverse().ToArray(), 0);

                leaves.Add().Serialize((writer, leaf) => {
                    leaf.Flags.Visit(writer, flags);
                    leaf.BSP2DReferenceCount.Visit(writer, (short)bsp2dRefCount);
                    leaf.Unknown.Visit(writer, (short)Unknown);
                    leaf.FirstBSP2DReference.Visit(writer, first2dRef);
                });
            }

            return originalPos + (count * LEAF_SIZE);
        }

        protected long ParseBSP2DReferences(
            ContainerBuilder.InstanceSerializationContext<IBSP> bsp,
            BinaryReader reader, int count)
        {
            var bsp2dReferences = bsp.GetSerializationContext((bsp2) => bsp2.BSP2DReferences);
            var originalPos = reader.BaseStream.Position;

            for (uint i = 0; i < count; ++i)
            {
                reader.BaseStream.Position = originalPos + (i * BSP2DREFERENCE_SIZE);
                var plane_idx = BitConverter.ToInt32(reader.ReadBytes(4).Reverse().ToArray(), 0);
                var bsp2dnode_idx = BitConverter.ToInt32(reader.ReadBytes(4).Reverse().ToArray(), 0);

                bsp2dReferences.Add().Serialize((writer, bsp2dRef) => {
                    //truncate and preserve sign
                    var uplane_idx = (plane_idx & 0x7fff);
                    if (plane_idx < 0)
                        uplane_idx |= 0x8000;

                    bsp2dRef.PlaneIndex.Visit(writer, (short)uplane_idx);

                    var ubsp2dnode_idx = (bsp2dnode_idx & 0x7fff);
                    if (bsp2dnode_idx < 0)
                        ubsp2dnode_idx |= 0x8000;

                    bsp2dRef.BSP2DNodeIndex.Visit(writer, (short)ubsp2dnode_idx);
                });
            }

            return originalPos + (count * BSP2DREFERENCE_SIZE);
        }

        protected long ParseBSP2DNodes(
            ContainerBuilder.InstanceSerializationContext<IBSP> bsp,
            BinaryReader reader, int count)
        {
            var bsp2dNodes = bsp.GetSerializationContext((bsp2) => bsp2.BSP2DNodes);
            var originalPos = reader.BaseStream.Position;

            for (uint i = 0; i < count; ++i)
            {
                reader.BaseStream.Position = originalPos + (i * BSP2DNODE_SIZE);
                var plane_i = BitConverter.ToSingle(reader.ReadBytes(4).Reverse().ToArray(), 0);
                var plane_j = BitConverter.ToSingle(reader.ReadBytes(4).Reverse().ToArray(), 0);
                var plane_d = BitConverter.ToSingle(reader.ReadBytes(4).Reverse().ToArray(), 0);
                var left_child = BitConverter.ToInt32(reader.ReadBytes(4).Reverse().ToArray(), 0);
                var right_child = BitConverter.ToInt32(reader.ReadBytes(4).Reverse().ToArray(), 0);

                bsp2dNodes.Add().Serialize((writer, bsp2dNode) => {
                    bsp2dNode.Plane2DEquationIJD.Visit(writer, new float[] {
                        plane_i, plane_j, plane_d
                    });

                    //sign-compress left and right children to int16
                    var uleft_child = left_child & 0x7fff;
                    if (left_child < 0)
                        uleft_child |= 0x8000;

                    bsp2dNode.LeftChild.Visit(writer, (short)uleft_child);

                    var uright_child = right_child & 0x7fff;
                    if (right_child < 0)
                        uright_child |= 0x8000;

                    bsp2dNode.RightChild.Visit(writer, (short)uright_child);
                });
            }

            return originalPos + (count * BSP2DNODE_SIZE);
        }

        protected long ParseSurfaces(
            ContainerBuilder.InstanceSerializationContext<IBSP> bsp,
            BinaryReader reader, int count)
        {
            var surfaces = bsp.GetSerializationContext((bsp2) => bsp2.Surfaces);
            var originalPos = reader.BaseStream.Position;

            for (uint i = 0; i < count; ++i)
            {
                reader.BaseStream.Position = originalPos + (i * SURFACE_SIZE);
                var plane_idx = BitConverter.ToInt32(reader.ReadBytes(4).Reverse().ToArray(), 0);
                var first_edge = BitConverter.ToInt32(reader.ReadBytes(4).Reverse().ToArray(), 0);
                var flags = reader.ReadByte();
                var breakable_surface = reader.ReadByte();
                var material = BitConverter.ToInt16(reader.ReadBytes(2).Reverse().ToArray(), 0);

                surfaces.Add().Serialize((writer, surface) =>
                {
                    surface.PlaneIndex.Visit(writer, (short)plane_idx);
                    surface.FirstEdgeIndex.Visit(writer, (short)first_edge);
                    surface.MaterialIndex.Visit(writer, (short)material);
                    surface.BreakableSurface.Visit(writer, (short)breakable_surface);
                    surface.Flags.Visit(writer, (ushort)flags);
                });
            }
            return originalPos + (count * SURFACE_SIZE);
        }

        protected long ParseEdges(
            ContainerBuilder.InstanceSerializationContext<IBSP> bsp,
            BinaryReader reader, int count)
        {
            var edges = bsp.GetSerializationContext((bsp2) => bsp2.Edges);
            var originalPos = reader.BaseStream.Position;

            for (uint i = 0; i < count; ++i)
            {
                reader.BaseStream.Position = originalPos + (i * EDGE_SIZE);
                var start_vert_idx = BitConverter.ToInt32(reader.ReadBytes(4).Reverse().ToArray(), 0);
                var end_vert_idx = BitConverter.ToInt32(reader.ReadBytes(4).Reverse().ToArray(), 0);
                var forward_edge_idx = BitConverter.ToInt32(reader.ReadBytes(4).Reverse().ToArray(), 0);
                var reverse_edge_idx = BitConverter.ToInt32(reader.ReadBytes(4).Reverse().ToArray(), 0);
                var left_surface_idx = BitConverter.ToInt32(reader.ReadBytes(4).Reverse().ToArray(), 0);
                var right_surface_idx = BitConverter.ToInt32(reader.ReadBytes(4).Reverse().ToArray(), 0);

                edges.Add().Serialize((writer, edge) => {
                    edge.StartVertexIndex.Visit(writer, (short)start_vert_idx);
                    edge.EndVertexIndex.Visit(writer, (short)end_vert_idx);
                    edge.ForwardEdgeIndex.Visit(writer, (short)forward_edge_idx);
                    edge.ReverseEdgeIndex.Visit(writer, (short)reverse_edge_idx);
                    edge.LeftSurfaceIndex.Visit(writer, (short)left_surface_idx);
                    edge.RightSurfaceIndex.Visit(writer, (short)right_surface_idx);
                });
            }

            return originalPos + (count * EDGE_SIZE);
        }

        protected long ParseVertices(
            ContainerBuilder.InstanceSerializationContext<IBSP> bsp,
            BinaryReader reader, int count)
        {
            var vertices = bsp.GetSerializationContext((bsp2) => bsp2.Vertices);
            var originalPos = reader.BaseStream.Position;

            for (uint i = 0; i < count; ++i)
            {
                reader.BaseStream.Position = originalPos + (i * VERTEX_SIZE);
                var point_x = BitConverter.ToSingle(reader.ReadBytes(4).Reverse().ToArray(), 0);
                var point_y = BitConverter.ToSingle(reader.ReadBytes(4).Reverse().ToArray(), 0);
                var point_z = BitConverter.ToSingle(reader.ReadBytes(4).Reverse().ToArray(), 0);
                var first_edge = BitConverter.ToInt32(reader.ReadBytes(4).Reverse().ToArray(), 0);

                vertices.Add().Serialize((writer, vert) =>
                {
                    vert.PointXYZ.Visit(writer, new float[] { point_x, point_y, point_z });

                    vert.FirstEdgeIndex.Visit(writer, (short)first_edge);
                });
            }

            return originalPos + (count * VERTEX_SIZE);
        }

        protected long ParseMain(
            ContainerBuilder.RootSerializationContext<ICollisionModel> coll,
            BinaryReader reader)
        {
            // start of the main struct in the reader
            var originalPos = reader.BaseStream.Position;
            //location of the count of materials
            reader.BaseStream.Position += MAIN_MATERIAL_OFFSET;
            var n_materials = BitConverter.ToInt32(reader.ReadBytes(4).Reverse().ToArray(), 0);
            //change pos to the offset where the 'material' tagblocks begin
            reader.BaseStream.Position = originalPos + MAIN_REGION_OFFSET;
            var n_regions = BitConverter.ToInt32(reader.ReadBytes(4).Reverse().ToArray(), 0);
            reader.BaseStream.Position = originalPos + MAIN_PATHF_SPHERES_OFFSET;
            var n_pathf_spheres = BitConverter.ToInt32(reader.ReadBytes(4).Reverse().ToArray(), 0);
            reader.BaseStream.Position = originalPos + MAIN_NODES_OFFSET;
            var n_nodes = BitConverter.ToInt32(reader.ReadBytes(4).Reverse().ToArray(), 0);

            var afterReadPos = originalPos + MAIN_STRUCT_SIZE;
            //get the position after all of the materials
            reader.BaseStream.Position = afterReadPos;
            afterReadPos = ParseMaterials(coll, reader, n_materials);

            //Get the position after all of the sequentially stored regions and sequentially stored permutations
            reader.BaseStream.Position = afterReadPos;

            afterReadPos = ParseRegions(coll, reader, n_regions);
            //set to beginning of sequential list of path finding spheres.
            reader.BaseStream.Position = afterReadPos;
            afterReadPos = ParsePathFindingSpheres(coll, reader, n_pathf_spheres);

            reader.BaseStream.Position = afterReadPos;
            afterReadPos = ParseNodes(coll, reader, n_nodes);

            return afterReadPos;
        }

        /// <summary>
        /// This file parser will parse Halo 1 CE 'model_collision_geometry' tags.
        /// The addresses of the tagblocks inside the tag are likely to be garbage
        /// values. The Halo 1 CE development tool 'guerilla' does not use the 
        /// reflexive address value and expects chunks to occur in the order that
        /// the reflexives occur in the parent struct.
        /// 
        /// The Halo1 CE collision tag is used due to high compatibility and 
        /// availability of 'Tool' - a program which can compile collision tags.
        /// 
        /// The parser expects the following format:
        /// h1ce coll tag format:
        ///main struct
        ///all materials sequential
        ///all regions sequential
        ///all permutations sequential
        ///all path finding spheres sequential
        ///all nodes sequential
        ///bsp 0
        ///	   bsp0 3dnodes sequential
        ///	   ...
        ///	   bsp0 vertices sequential
        ///bsp 1
        ///	   ...
        ///...
        /// </summary>
        /// <param name="fpath"></param>
        /// <returns></returns>
        public bool ParseFromFile(ICollisionModel definition, ICacheContext context, string fpath)
        {

            FileStream fs = null;
            try
            {
                fs = new FileStream(fpath, FileMode.Open, FileAccess.Read);
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("The system cannot find the file specified.");
                return false;
            }

            var reader = new BinaryReader(fs);

            // h1 ce tags will have a 64 byte header. The main struct is immediately after.

            var len = reader.BaseStream.Length;
            reader.BaseStream.Position = MAIN_STRUCT_OFFSET;
            var coll = ContainerBuilder.CreateSerializationContext(definition, context);
            var size = ParseMain(coll, reader);

            if (len != size)
            {
                Console.WriteLine("length expected was not actual.\nexpected: " + len + ", actual: " + size);
                return false;
            }

            RootSerializationContext = coll;
            return true;
        }

        public DataBlock[] Build()
        {
            return RootSerializationContext.Finish();
        }

    }
}
