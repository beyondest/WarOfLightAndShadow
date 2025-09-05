using System;
using SparFlame.Components.SubGameplay;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.SubGameplay.EnemyAI
{
    public class EnemyInitDistinguishSystemAuthoring : MonoBehaviour
    {
        public AIInitDistinguishConfig config;
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
    public struct AIInitDistinguishConfig : IComponentData
    {
         [Sirenix.OdinInspector.ReadOnly]
         public int aiTeamTypeCount;
    }

    
    // Enemy Base Specific Data


    public struct AIBaseBelongsTo : IComponentData
    {
        public Entity BuildingPack;
    }
    public struct AIBaseTeamGeneralData : IBufferElementData
    {
        public AITeamType TeamType;
        public int CurCount;
    }

    public struct AIBaseGarrisonTowerData : IBufferElementData
    {
        public Entity Tower;
        public int AvailableCount;
    }
   
  
}