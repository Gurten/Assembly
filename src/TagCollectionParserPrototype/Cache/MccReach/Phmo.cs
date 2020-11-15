/// The Tag Collection Parser Prototype Project
/// Author: Gurten
using System;
using TagCollectionParserPrototype.Cache.Core;
using TagCollectionParserPrototype.Cache.Types.Phmo;

namespace TagCollectionParserPrototype.Cache.MccReach.Phmo
{
    public class PhysicsModelShapeTypes : IPhysicsModelShapeTypes
    {
        public ConfigConstant<UInt16> Polyhedron => new ConfigConstant<UInt16>(4);
        public ConfigConstant<UInt16> List => new ConfigConstant<UInt16>(0xe);
    }

    public class PhysicsModelMotionTypes : IPhysicsModelMotionTypes
    {
        ConfigConstant<UInt16> IPhysicsModelMotionTypes.Keyframed => new ConfigConstant<UInt16>(4);

        ConfigConstant<UInt16> IPhysicsModelMotionTypes.Fixed => new ConfigConstant<UInt16>(5);
    }

    public class PhysicsModelRigidBodyRuntimeFlags : IPhysicsModelRigidBodyRuntimeFlags
    {
        // Makes things collidable!
        ConfigConstant<UInt16> IPhysicsModelRigidBodyRuntimeFlags.SuperImportantFlag 
            => new ConfigConstant<UInt16>(0x02); // Bit 1
    }
}
