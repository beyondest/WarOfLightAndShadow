using System;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.EnemyAI
{
    public class EnemyDistinguishConfigAuthoring : MonoBehaviour
    {
     
        private class EnemyInitSystemAuthoringBaker : Baker<EnemyDistinguishConfigAuthoring>
        {
            public override void Bake(EnemyDistinguishConfigAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                
                AddComponent(entity, new EnemyInitConfig
                {
                    AITeamTypeCount = Enum.GetValues(typeof(AITeamType)).Length
                });
            }
        }
    }


    public struct EnemyInitConfig : IComponentData
    {
        public int AITeamTypeCount;
    }

  
}