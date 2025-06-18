using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.SubGameplay.EnemyAI
{
    public class EnemyUnitAssignSystemAuthoring : MonoBehaviour
    {

        private class EnemyTeamManageSystemAuthoringBaker : Baker<EnemyUnitAssignSystemAuthoring>
        {
            public override void Bake(EnemyUnitAssignSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new EnemyUnitAssignSystemConfig
                {

                });
            }
        }
    }

    public struct EnemyUnitAssignSystemConfig : IComponentData
    {

    }
    
  
  


}