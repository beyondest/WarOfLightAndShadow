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


    
    
    
}