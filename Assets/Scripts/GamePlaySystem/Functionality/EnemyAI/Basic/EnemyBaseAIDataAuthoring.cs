using System;
using System.Collections.Generic;
using SparFlame.GamePlaySystem.Units;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SparFlame.GamePlaySystem.EnemyAI
{
    public class EnemyBaseAIDataAuthoring : MonoBehaviour
    {
        public List<TeamTypeToAssembleRef> teamTypeToAssembles;
        public GameObject fallBackPosRef;
        public float defenseRadius;
        public List<GameObject> garrisonTowers;
        
        private class EnemyBaseAIDataAuthoringBaker : Baker<EnemyBaseAIDataAuthoring>
        {
            public override void Bake(EnemyBaseAIDataAuthoring authoring)
            {
                float3 baseWorldPos = authoring.transform.position;

                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new EnemyBasePosData
                {
                    FallBackPosBias =(float3) authoring.fallBackPosRef.transform.position - baseWorldPos,
                    DefenseRadius = authoring.defenseRadius
                });
                
                // Bake assembleLocs bias
                var buffer = AddBuffer<EnemyBaseAssembleLocs>(entity);
                var count = 0;
                foreach (var entry in authoring.teamTypeToAssembles)
                {
                    if (entry.teamType != (AITeamType)count)
                        throw new ArgumentException("Team type must be in enum type order");
                    count++;
                    if (entry.locationRef == null)
                        continue;
                    float3 targetPos = entry.locationRef.transform.position;
                    var offset = targetPos - baseWorldPos;
                    buffer.Add(new EnemyBaseAssembleLocs
                    {
                        TeamAssembleLocationBias = offset
                    });
                }
                
                // Bake garrison tower datas
                var buffer2 = AddBuffer<EnemyBaseGarrisonTowerData>(entity);
                foreach (var tower in authoring.garrisonTowers)
                {
                    buffer2.Add(new EnemyBaseGarrisonTowerData
                    {
                        Tower = GetEntity(tower, TransformUsageFlags.Dynamic)
                    });
                }
                

            }
        }
    }

    
    [Serializable]
    public struct TeamTypeToAssembleRef
    {
        public AITeamType teamType;
        public GameObject locationRef;
    }

    public struct EnemyBaseAssembleLocs : IBufferElementData
    {
        public float3 TeamAssembleLocationBias;
    }

    public struct EnemyBaseGarrisonTowerData : IBufferElementData
    {
        public Entity Tower;
        public int AvailableCount;
    }

    public struct EnemyBasePosData : IComponentData
    {
        public float3 FallBackPosBias;
        public float DefenseRadius;
    }
    
    // Enemy Base Specific Data
    
    public struct EnemyBaseTeamGeneralData : IBufferElementData
    {
        public AITeamType TeamType;
        public int CurCount;
    }

    public struct EnemyBaseTeamAvailableData : IBufferElementData
    {
        public AITeamType TeamType; // gather / attack / defense / harass
        public Entity TeamEntity; 
        public FixedList128Bytes<MemberCountEntry> AvailableMemberCountEntries;
    }
    
}