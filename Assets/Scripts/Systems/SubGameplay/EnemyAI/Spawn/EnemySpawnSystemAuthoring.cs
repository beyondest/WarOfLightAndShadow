using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.SubGameplay.EnemyAI
{
    public class EnemySpawnSystemAuthoring : MonoBehaviour
    {
        private class EnemySpawnSystemAuthoringBaker : Baker<EnemySpawnSystemAuthoring>
        {
            public override void Bake(EnemySpawnSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new EnemySpawnSystemConfig
                {
                });
            }
        }
    }

    public struct EnemySpawnSystemConfig : IComponentData
    {
    }


    
   

}