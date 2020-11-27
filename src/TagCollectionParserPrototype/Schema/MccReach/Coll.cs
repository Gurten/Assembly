/// The Tag Collection Parser Prototype Project
/// Author: Gurten
using System;
using TagCollectionParserPrototype.Schema.Core;
using TagCollectionParserPrototype.Schema.Coll;
using Blamite.Blam;
using TagCollectionParserPrototype.Schema.Coll.region;
using TagCollectionParserPrototype.Schema.Coll.region.permutation;
using TagCollectionParserPrototype.Schema.Coll.region.permutation.bsp;

namespace TagCollectionParserPrototype.Schema.MccReach.Coll
{
	class Material : IMaterial
	{
		DataField<StringID> IMaterial.Name => new DataField<StringID>(0);

        uint IStructSchema.Size => 4;

        uint IStructSchema.Alignment => 4;
    }

	class Region : IRegion
	{
        DataField<StringID> IRegion.Name => new DataField<StringID>(0);

        ITagBlockRef<IPermutation> IRegion.Permutations 
            => new NewGenTagBlockRef<IPermutation>(4, new region.Permutation());

        uint IStructSchema.Size => 0x10;

        uint IStructSchema.Alignment => 4;
    }

	namespace region
	{
		class Permutation : IPermutation
		{

            DataField<StringID> IPermutation.Name => new DataField<StringID>(0);

            ITagBlockRef<IBSP> IPermutation.BSPs 
                => new NewGenTagBlockRef<IBSP>(4, new permutation.BSP());

            uint IStructSchema.Size => 0x28;

            uint IStructSchema.Alignment => 4;
        }

		namespace permutation
		{
			class BSP : IBSP
			{

                DataField<Int16> IBSP.NodeIndex => new DataField<Int16>(0);

                ITagBlockRef<IBSP3DNode> IBSP.BSP3DNodes 
                    => new NewGenTagBlockRef<IBSP3DNode>(4, new bsp.BSP3DNode());

                // Something here, missing. 

                ITagBlockRef<IPlane> IBSP.Planes
                    => new NewGenTagBlockRef<IPlane>(0x1c, new bsp.Plane());

                ITagBlockRef<ILeafNode> IBSP.Leaves
                    => new NewGenTagBlockRef<ILeafNode>(0x28, new bsp.LeafNode());

                ITagBlockRef<IBSP2DReference> IBSP.BSP2DReferences
                    => new NewGenTagBlockRef<IBSP2DReference>(0x34, new bsp.BSP2DReference());

                ITagBlockRef<IBSP2DNode> IBSP.BSP2DNodes
                    => new NewGenTagBlockRef<IBSP2DNode>(0x40, new bsp.BSP2DNode());

                ITagBlockRef<ISurface> IBSP.Surfaces
                    => new NewGenTagBlockRef<ISurface>(0x4c, new bsp.Surface());

                ITagBlockRef<IEdge> IBSP.Edges
                    => new NewGenTagBlockRef<IEdge>(0x58, new bsp.Edge());

                ITagBlockRef<IVertex> IBSP.Vertices
                    => new NewGenTagBlockRef<IVertex>(0x64, new bsp.Vertex());

                uint IStructSchema.Size => 0x70;

                uint IStructSchema.Alignment => 4;
            }

			namespace bsp
			{
                class BSP3DNode : IBSP3DNode
                {
                    VectorField<byte> IBSP3DNode.BackChild => new VectorField<byte>(0, 3);

                    VectorField<byte> IBSP3DNode.FrontChild => new VectorField<byte>(0, 3);

                    DataField<Int16> IBSP3DNode.PlaneIndex => new DataField<Int16>(6);

                    uint IStructSchema.Size => 8;

                    uint IStructSchema.Alignment => 8;
                }

                class Plane : IPlane
                {
                    VectorField<float> IPlane.EquationIJKD => new VectorField<float>(0, 4);

                    uint IStructSchema.Size => 0x10;

                    uint IStructSchema.Alignment => 0x10;
                }

                class LeafNode : ILeafNode
                {
                    DataField<Int16> ILeafNode.Flags => new DataField<Int16>(0);

                    DataField<Int16> ILeafNode.BSP2DReferenceCount => new DataField<Int16>(2);

                    DataField<Int16> ILeafNode.Unknown => new DataField<Int16>(4);

                    DataField<Int16> ILeafNode.FirstBSP2DReference => new DataField<Int16>(6);

                    uint IStructSchema.Size => 8;

                    uint IStructSchema.Alignment => 8;
                }

                class BSP2DReference : IBSP2DReference
                {
                    DataField<Int16> IBSP2DReference.PlaneIndex => new DataField<Int16>(0);

                    DataField<Int16> IBSP2DReference.BSP2DNodeIndex => new DataField<Int16>(2);

                    // Size is 4 in the xml, but alignment is 8?
                    uint IStructSchema.Size => 8;

                    uint IStructSchema.Alignment => 8;
                }

                class BSP2DNode : IBSP2DNode
                {
                    VectorField<float> IBSP2DNode.Plane2DEquationIJD => new VectorField<float>(0, 3);

                    DataField<Int16> IBSP2DNode.LeftChild => new DataField<Int16>(0xc);

                    DataField<Int16> IBSP2DNode.RightChild => new DataField<Int16>(0xe);

                    uint IStructSchema.Size => 0x10;

                    uint IStructSchema.Alignment => 0x10;
                }

                class Surface : ISurface
                {
                    DataField<Int16> ISurface.PlaneIndex => new DataField<Int16>(0);

                    DataField<Int16> ISurface.FirstEdgeIndex => new DataField<Int16>(2);

                    DataField<Int16> ISurface.MaterialIndex => new DataField<Int16>(4);

                    DataField<Int16> ISurface.BreakableSurface => new DataField<Int16>(8);
                    DataField<UInt16> ISurface.Flags => new DataField<UInt16>(0xa);

                    uint IStructSchema.Size => 0xc;

                    uint IStructSchema.Alignment => 4;
                }

                class Edge : IEdge
                {
                    DataField<Int16> IEdge.StartVertexIndex => new DataField<Int16>(0);

                    DataField<Int16> IEdge.EndVertexIndex => new DataField<Int16>(2);

                    DataField<Int16> IEdge.ForwardEdgeIndex => new DataField<Int16>(4);

                    DataField<Int16> IEdge.ReverseEdgeIndex => new DataField<Int16>(6);

                    DataField<Int16> IEdge.LeftSurfaceIndex => new DataField<Int16>(8);

                    DataField<Int16> IEdge.RightSurfaceIndex => new DataField<Int16>(0xa);

                    uint IStructSchema.Size => 0xc;

                    uint IStructSchema.Alignment => 4;
                }

                class Vertex : IVertex
                {
                    VectorField<float> IVertex.PointXYZ => new VectorField<float>(0, 3);

                    DataField<Int16> IVertex.FirstEdgeIndex => new DataField<Int16>(0xc);

                    uint IStructSchema.Size => 0x10;

                    uint IStructSchema.Alignment => 0x10;
                }
            }
		}
	}

    class PathFindingSphere : IPathFindingSphere
    {
        DataField<Int16> IPathFindingSphere.NodeIndex => new DataField<Int16>(0);

        DataField<UInt16> IPathFindingSphere.Flags => new DataField<UInt16>(2);

        VectorField<float> IPathFindingSphere.SphereLocation => new VectorField<float>(4, 3);

        DataField<float> IPathFindingSphere.SphereRadius => new DataField<float>(0x10);

        uint IStructSchema.Size => 0x14;

        uint IStructSchema.Alignment => 4;
    }

    class Node : INode
    {
        DataField<StringID> INode.Name => new DataField<StringID>(0);

        DataField<Int16> INode.Unknown => new DataField<Int16>(4);

        DataField<Int16> INode.ParentNodeIndex => new DataField<Int16>(6);

        DataField<Int16> INode.NextSiblingNodeIndex => new DataField<Int16>(8);

        DataField<Int16> INode.FirstChildNodeIndex => new DataField<Int16>(0xa);

        uint IStructSchema.Size => 0xc;

        uint IStructSchema.Alignment => 4;
    }


    class MCCReachCollisionModel : ICollisionModel
    {
        ITagBlockRef<IMaterial> ICollisionModel.Materials
            => new NewGenTagBlockRef<IMaterial>(0x14, new Material());

        ITagBlockRef<IRegion> ICollisionModel.Regions 
            => new NewGenTagBlockRef<IRegion>(0x20, new Region());

        ITagBlockRef<IPathFindingSphere> ICollisionModel.PathFindingSpheres
            => new NewGenTagBlockRef<IPathFindingSphere>(0x38, new PathFindingSphere());

        ITagBlockRef<INode> ICollisionModel.Nodes 
            => new NewGenTagBlockRef<INode>(0x44, new Node());

        uint IStructSchema.Size => 0x54;

        uint IStructSchema.Alignment => 4;
    }

}