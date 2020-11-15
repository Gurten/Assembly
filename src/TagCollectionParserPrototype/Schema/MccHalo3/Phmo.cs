/// The Tag Collection Parser Prototype Project
/// Author: Gurten
using Blamite.Blam;
using Blamite.IO;
using System;
using TagCollectionParserPrototype.Schema.Core;
using TagCollectionParserPrototype.Schema.Phmo;

namespace TagCollectionParserPrototype.Schema.MccHalo3.Phmo
{
    class MCCHalo3PhysicsModelMaterial : IPhysicsModelMaterial
    {
        UInt32 IStructSchema.Size => 0xC;
        UInt32 IStructSchema.Alignment => 4;

        DataField<StringID> IPhysicsModelMaterial.Name => new DataField<StringID>(0);
        DataField<Int16> IPhysicsModelMaterial.PhantomTypeIndex => new DataField<Int16>(0x8);
    }

    class MCCHalo3PhysicsModelPolyhedra : IPhysicsModelPolyhedra, IStructWithDataFixup
    {
        public UInt32 Size => 0xA0;
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
            => new NewGenSizeAndCapacityField(0x78);
        DataField<Int32> IPhysicsModelPolyhedra.VertexCount => new DataField<Int32>(0x80);
        ISizeAndCapacityField IPhysicsModelPolyhedra.PlaneEquations
            => new NewGenSizeAndCapacityField(0x90);

        public void VisitInstance(IWriter writer, uint index)
        {
            writer.SeekTo(this.InstanceOffset[0].Offset);
            writer.WriteUInt64(32 + this.Size * index);
        }
    }

    class MCCHalo3PhysicsModelLists : IPhysicsModelLists, IStructWithDataFixup
    {
        UInt32 IStructSchema.Size => 96;
        UInt32 IStructSchema.Alignment => 0x10;

        public DataField<uint> Count => new DataField<uint>(0xA);
        ISizeAndCapacityField IPhysicsModelLists.ChildShapes => new NewGenSizeAndCapacityField(0x38);
        VectorField<float> IPhysicsModelLists.AABBHalfExtents => new VectorField<float>(0x40, 4);
        VectorField<float> IPhysicsModelLists.AABBCenter => new VectorField<float>(0x50, 4);

        void IStructWithDataFixup.VisitInstance(IWriter writer, uint index)
        {
            Count.Visit(writer, 128);
        }
    }

    class MCCHalo3PhysicsModelListShapes : IPhysicsModelListShapes
    {
        UInt32 IStructSchema.Size => 0x20;
        UInt32 IStructSchema.Alignment => 0x4;

        DataField<ushort> IPhysicsModelListShapes.ShapeType => new DataField<UInt16>(0);
        DataField<ushort> IPhysicsModelListShapes.ShapeIndex => new DataField<UInt16>(2);
        DataField<ushort> IPhysicsModelListShapes.ChildShapeCount => new DataField<UInt16>(0x10);
    }



    class PhysicsModelFourVectors : IPhysicsModelFourVectors
    {
        UInt32 IStructSchema.Size => 0x30;
        UInt32 IStructSchema.Alignment => 0x10;
        VectorField<float> IPhysicsModelFourVectors.Xs => new VectorField<float>(0, 4);
        VectorField<float> IPhysicsModelFourVectors.Ys => new VectorField<float>(0x10, 4);
        VectorField<float> IPhysicsModelFourVectors.Zs => new VectorField<float>(0x20, 4);
    }


    class PhysicsModelPlaneEquations : IPhysicsModelPlaneEquations
    {
        UInt32 IStructSchema.Size => 0x10;
        UInt32 IStructSchema.Alignment => 0x10;
        VectorField<float> IPhysicsModelPlaneEquations.PlaneEquation => new VectorField<float>(0, 4);
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

    class MCCHalo3PhysicsModelRigidBody : IPhysicsModelRigidBody
    {
        UInt32 IStructSchema.Size => 0xc0;
        UInt32 IStructSchema.Alignment => 0x10;

        DataField<float> IPhysicsModelRigidBody.BoundingSphereRadius 
            => new DataField<float>(0x14);
        VectorField<byte> IPhysicsModelRigidBody.MotionType 
            => new VectorField<byte>(0x1a, 2);
        DataField<UInt16> IPhysicsModelRigidBody.ShapeType => new DataField<UInt16>(0x58);
        DataField<UInt16> IPhysicsModelRigidBody.ShapeIndex => new DataField<UInt16>(0x5A);
        DataField<float> IPhysicsModelRigidBody.Mass => new DataField<float>(0x60);
        DataField<UInt16> IPhysicsModelRigidBody.RuntimeFlags => new DataField<UInt16>(0xBE);
        
    }

    class MCCHalo3PhysicsModel : IPhysicsModel
    {
        UInt32 IStructSchema.Size => 0x198;
        UInt32 IStructSchema.Alignment => 4;

        ITagBlockRef<IPhysicsModelRigidBody> IPhysicsModel.RigidBodyTagBlock
            => new NewGenTagBlockRef<IPhysicsModelRigidBody>(0x58, new MCCHalo3PhysicsModelRigidBody());
        ITagBlockRef<IPhysicsModelMaterial> IPhysicsModel.MaterialsTagBlock
            => new NewGenTagBlockRef<IPhysicsModelMaterial>(0x64, new MCCHalo3PhysicsModelMaterial());
        ITagBlockRef<IPhysicsModelPolyhedra> IPhysicsModel.PolyhedraTagBlock
            => new NewGenTagBlockRef<IPhysicsModelPolyhedra>(0xac, new MCCHalo3PhysicsModelPolyhedra());
        ITagBlockRef<IPhysicsModelFourVectors> IPhysicsModel.PolyhedraFourVectorTagBlock
            => new NewGenTagBlockRef<IPhysicsModelFourVectors>(0xb8, new PhysicsModelFourVectors());
        ITagBlockRef<IPhysicsModelPlaneEquations> IPhysicsModel.PolyhedraPlaneEquationsTagBlock
            => new NewGenTagBlockRef<IPhysicsModelPlaneEquations>(0xc4, new PhysicsModelPlaneEquations());
        ITagBlockRef<IPhysicsModelLists> IPhysicsModel.ListsTagBlock
            => new NewGenTagBlockRef<IPhysicsModelLists>(0xdc, new MCCHalo3PhysicsModelLists());
        ITagBlockRef<IPhysicsModelListShapes> IPhysicsModel.ListsShapesTagBlock
            => new NewGenTagBlockRef<IPhysicsModelListShapes>(0xe8, new MCCHalo3PhysicsModelListShapes());
        //ITagBlockRef<IPhysicsModelListShapes> IPhysicsModel.RegionsTagBlock => throw new NotImplementedException();
        ITagBlockRef<IPhysicsModelNode> IPhysicsModel.NodesTagBlock
            => new NewGenTagBlockRef<IPhysicsModelNode>(0x138, new PhysicsModelNode());
    }
}
