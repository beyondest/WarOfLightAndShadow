using System;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Units;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SparFlame.GamePlaySystem.EnemyAI
{
    public class EnemyUnitCommandSystemAuthoring : MonoBehaviour
    {
        public EnemyUnitCommandSystemConfig config;
        private class EnemyAISystemAuthoringBaker : Baker<EnemyUnitCommandSystemAuthoring>
        {
            public override void Bake(EnemyUnitCommandSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, authoring.config);
            }
        }
    }

    public enum EnemyCommandType
    {
        None = 0,
        March = 1,
        Garrison = 2,
        Attack = 3
    }
    
    [Serializable]
    public struct EnemyUnitCommandSystemConfig : IComponentData
    {
        public float aiMarchExtent;
    }

    public struct EnemyUnitCommandData : IComponentData
    {
        public Entity TargetEntity;
        public EnemyCommandType CommandType;
        public float3 TargetPos;
        public bool Focus;
    }

    public struct EnemyUnitCommandUpdate : IComponentData, IEnableableComponent
    {
        
    }
 
}