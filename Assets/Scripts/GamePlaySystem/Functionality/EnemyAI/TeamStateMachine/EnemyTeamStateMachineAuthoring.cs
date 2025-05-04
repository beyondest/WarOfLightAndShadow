using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

namespace SparFlame.GamePlaySystem.EnemyAI
{
    public class EnemyTeamStateMachineAuthoring : MonoBehaviour
    {

        private class EnemyTeamStateMachineAuthoringBaker : Baker<EnemyTeamStateMachineAuthoring>
        {
            public override void Bake(EnemyTeamStateMachineAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new EnemyTeamStateMachineConfig
                {
                    
                });
            }
        }
    }


    // This tag is controlled by team state machine and should always after
    public struct TeamNeedTargetTag : IComponentData, IEnableableComponent
    {
        
    }
    
    
    public struct TeamStateData : IComponentData
    {
        public Entity TargetEntity;
        public float3 TargetPosition;
        public bool Focus;
        public EnemyCommandType CommandType;
        public bool AssignTarget;
        public bool Idle;
    }

    [Serializable]
    public struct EnemyTeamStateMachineConfig : IComponentData
    {
        public float targetBiasDis;
        public float reachTargetToleranceDisSq;
    }

}