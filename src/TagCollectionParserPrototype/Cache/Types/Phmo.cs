/// The Tag Collection Parser Prototype Project
/// Author: Gurten
using System;
using TagCollectionParserPrototype.Cache.Core;

namespace TagCollectionParserPrototype.Cache.Types.Phmo
{
    interface IPhysicsModelShapeTypes
    {
        //Add more as needed.
        ConfigConstant<UInt16> Polyhedron { get; }
        ConfigConstant<UInt16> List { get; }
    }

    interface IPhysicsModelMotionTypes
    {
        ConfigConstant<UInt16> Keyframed { get; }
        ConfigConstant<UInt16> Fixed { get; }
    }
    interface IPhysicsModelRigidBodyRuntimeFlags
    {
        //Add more as needed.
        ConfigConstant<UInt16> SuperImportantFlag { get; }
    }
}
