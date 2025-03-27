using Unity.Entities;

namespace Unity.Rendering
{
    [MaterialProperty("_PopNumberID")]
    struct PopNumberIDFloatOverride : IComponentData
    {
        public float Value;
    }
}
