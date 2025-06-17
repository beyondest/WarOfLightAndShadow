using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.EnemyAI
{
    public class PlayerUnitOutsideMonitorAuthoring : MonoBehaviour
    {
        public float outsideDisThreshold;
        private class PlayerUnitOutsideMonitorAuthoringBaker : Baker<PlayerUnitOutsideMonitorAuthoring>
        {
            public override void Bake(PlayerUnitOutsideMonitorAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new PlayerUnitOutsideMonitorConfig
                {
                    OutSideDisThresholdSq = authoring.outsideDisThreshold * authoring.outsideDisThreshold,
                });
            }
        }
    }
    public struct OutsideTag : IComponentData
    {
        public float OutSideDisSq; // The distance sq to nearest player base
    }

    public struct PlayerUnitOutsideMonitorConfig : IComponentData
    {
        public float OutSideDisThresholdSq;
    }
}