using SparFlame.Components.MainGameplay;
using SparFlame.Core.Utils;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.MainGameplay.ArmyGroup
{
    public class ArmyGroupBillboardConfigAuthoring : MonoBehaviour
    {
        public int armyGroupIconChildIndex;
        public int armyGroupSelectChildIndex;
        [ColorUsage(true,true)]
        public Color lightColor;
        [ColorUsage(true,true)]
        public Color darkColor;
        private class ArmyGroupBillboardConfigAuthoringBaker : Baker<ArmyGroupBillboardConfigAuthoring>
        {
            public override void Bake(ArmyGroupBillboardConfigAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new ArmyGroupBillboardConfig
                {
                    IconChildIndex = authoring.armyGroupIconChildIndex,
                    SelectChildIndex = authoring.armyGroupSelectChildIndex,
                    DarkColor = authoring.lightColor.ToFloat4(),
                    LightColor = authoring.lightColor.ToFloat4(),
                });
            }
        }
    }
}