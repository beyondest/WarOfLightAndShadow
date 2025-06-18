using System;
using SparFlame.Components.SubGameplay;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.SubGameplay.EnemyAI
{
    public class EnemyInitDistinguishSystemAuthoring : MonoBehaviour
    {
        public EnemyInitDistinguishConfig config;
        private class EnemyInitSystemAuthoringBaker : Baker<EnemyInitDistinguishSystemAuthoring>
        {
            public override void Bake(EnemyInitDistinguishSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                authoring.config.aiTeamTypeCount = Enum.GetValues(typeof(AITeamType)).Length;
                AddComponent(entity, authoring.config);
            }
        }
    }

    [Serializable]
    public struct EnemyInitDistinguishConfig : IComponentData
    {
         [Sirenix.OdinInspector.ReadOnly]
         public int aiTeamTypeCount;
    }

    
    // Enemy Base Specific Data


    public struct EnemyBaseBelongsTo : IComponentData
    {
        public Entity BuildingPack;
    }
    public struct EnemyBaseTeamGeneralData : IBufferElementData
    {
        public AITeamType TeamType;
        public int CurCount;
    }

    public struct EnemyBaseGarrisonTowerData : IBufferElementData
    {
        public Entity Tower;
        public int AvailableCount;
    }
   
  
}