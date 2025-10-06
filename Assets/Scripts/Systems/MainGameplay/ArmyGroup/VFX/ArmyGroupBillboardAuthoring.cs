using SparFlame.Components.MainGameplay;
using SparFlame.Core.Utils;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

namespace SparFlame.Systems.MainGameplay.ArmyGroup
{
    public class ArmyGroupBillboardAuthoring : MonoBehaviour
    {
        [ColorUsage(true, true)] public Color initColor;

        public ArmyGroupIconType initIconType = 0;

        private class ArmyGroupBillboardBaker : Baker<ArmyGroupBillboardAuthoring>
        {
            public override void Bake(ArmyGroupBillboardAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Renderable);
                AddComponent(entity,
                    new ArmyGroupBillboardImageIDFloatOverride
                    {
                        Value = (int)authoring.initIconType
                    });
                AddComponent(entity,
                    new ArmyGroupBillboardBaseColorOverride
                    {
                        Value = authoring.initColor.ToFloat4()
                    });
            }
        }
    }
}