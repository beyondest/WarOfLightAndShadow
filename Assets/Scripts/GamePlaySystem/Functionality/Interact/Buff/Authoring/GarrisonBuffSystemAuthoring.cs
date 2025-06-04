using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Interact.GamePlaySystem.Functionality.Interact.Buff.Authoring
{
    public class GarrisonBuffSystemAuthoring : MonoBehaviour
    {
        public float hpRegenerationInterval = 3;
        public float hpRegenerationAmountRatio = 0.01f;
        public int minCountToTriggerGarrisonBuff = 2;
        private class GarrisonBuffSystemAuthoringBaker : Baker<GarrisonBuffSystemAuthoring>
        {
            public override void Bake(GarrisonBuffSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new GarrisonBuffConfig
                {
                    HpRegenerationAmountRatio = authoring.hpRegenerationAmountRatio,
                    HpRegenerationInterval = authoring.hpRegenerationInterval,
                    MinCountToTriggerGarrisonBuff = authoring.minCountToTriggerGarrisonBuff,
                });
            }
        }
    }

    public struct GarrisonBuffConfig : IComponentData
    {
        public float HpRegenerationInterval;
        public float HpRegenerationAmountRatio;
        public int MinCountToTriggerGarrisonBuff;
    }
}