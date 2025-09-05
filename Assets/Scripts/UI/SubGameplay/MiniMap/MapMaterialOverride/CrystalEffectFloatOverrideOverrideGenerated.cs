using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Rendering
{
    [MaterialProperty("_CrystalEffect")]
    public struct CrystalEffectFloatOverride : IComponentData
    {
        public float Value;
    }
}
