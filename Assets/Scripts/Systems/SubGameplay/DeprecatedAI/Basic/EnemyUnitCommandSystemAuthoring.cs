using System;
using SparFlame.Components.SubGameplay;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SparFlame.Systems.SubGameplay.EnemyAI
{
    public class EnemyUnitCommandSystemAuthoring : MonoBehaviour
    {
        public AIUnitCommandSystemConfig config;
        private class EnemyAISystemAuthoringBaker : Baker<EnemyUnitCommandSystemAuthoring>
        {
            public override void Bake(EnemyUnitCommandSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, authoring.config);
            }
        }
    }
    

 
}