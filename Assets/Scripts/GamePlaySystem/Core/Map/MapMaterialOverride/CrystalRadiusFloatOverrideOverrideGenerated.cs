using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Rendering
{
    [MaterialProperty("_CrystalRadius")]
    public struct CrystalRadiusFloatOverride : IComponentData
    {
        public float Value;
    }
}
