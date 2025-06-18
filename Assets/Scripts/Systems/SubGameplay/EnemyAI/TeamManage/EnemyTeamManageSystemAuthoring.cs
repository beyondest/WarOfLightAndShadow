using SparFlame.Components.SubGameplay;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.SubGameplay.EnemyAI
{
    public class EnemyTeamManageSystemAuthoring : MonoBehaviour
    {
        public float totalCountShortHandRatio = 0.5f;
        private class EnemyTeamManageSystemAuthoringBaker : Baker<EnemyTeamManageSystemAuthoring>
        {
            public override void Bake(EnemyTeamManageSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new EnemyTeamManageSystemConfig
                {
                    TotalCountShortHandRatio = authoring.totalCountShortHandRatio
                });
            }
        }
    }

    public struct EnemyTeamManageSystemConfig : IComponentData
    {
        public float TotalCountShortHandRatio;
    }
    
    
    public struct TeamWaitTag : IComponentData
    {
        
    }
    
    public struct TeamData : IComponentData
    {
        public AITeamType TeamType;
        public int SpecialUnitCount;
        public bool ShortHanded;
        public Entity BelongsToBase;
    }



    
    public struct TeamEntityData : IBufferElementData
    {
        public Entity Unit;
    }
    
    
    
}