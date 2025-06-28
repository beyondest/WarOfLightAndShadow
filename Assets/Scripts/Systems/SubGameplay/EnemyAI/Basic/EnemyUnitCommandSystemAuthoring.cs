using System;
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
    
    [Serializable]
    public struct AIUnitCommandSystemConfig : IComponentData
    {
        public float aiMarchExtent;
    }
    public enum AICommandType
    {
        None = 0,
        March = 1,
        Garrison = 2,
        Attack = 3
    }


    public struct AIUnitCommandData : IComponentData
    {
        public Entity TargetEntity;
        public AICommandType CommandType;
        public float3 TargetPos;
        public bool Focus;
    }

    public struct AIUnitCommandUpdate : IComponentData, IEnableableComponent
    {
        
    }
 
}