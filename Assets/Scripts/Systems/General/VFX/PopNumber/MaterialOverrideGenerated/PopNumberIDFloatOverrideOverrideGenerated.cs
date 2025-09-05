using Unity.Entities;

namespace Unity.Rendering
{
    [MaterialProperty("_PopNumberID")]
    public struct PopNumberIDFloatOverride : IComponentData
    {
        public float Value;
    }
}
