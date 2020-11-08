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
        ConfigConstant<byte> IPhysicsModelMotionTypes.Keyframed => new ConfigConstant<byte>(4);

        ConfigConstant<byte> IPhysicsModelMotionTypes.Fixed => new ConfigConstant<byte>(5);
    }
}
