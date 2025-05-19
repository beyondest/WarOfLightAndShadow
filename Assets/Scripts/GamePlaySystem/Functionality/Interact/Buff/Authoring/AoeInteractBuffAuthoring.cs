using SparFlame.GamePlaySystem.EnemyAI;
using SparFlame.GamePlaySystem.General;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Interact
{
    public class AoeInteractBuffAuthoring : MonoBehaviour
    {
        public GameObject aoeTriggerPrefab;
        public float triggerDuration;
        public int maxTriggerCount;
        private class AoeAttackBuffAuthoringBaker : Baker<AoeInteractBuffAuthoring>
        {
            public override void Bake(AoeInteractBuffAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddBuffer<AoeTarget>(entity);
                AddComponent(entity, new AoeTriggerRequest
                {
                    Prefab = GetEntity(authoring.aoeTriggerPrefab, TransformUsageFlags.Dynamic)
                });
                AddComponent(entity, new AoeInteractData
                {
                    MaxTriggerCount = authoring.maxTriggerCount,
                    TriggerDuration = authoring.triggerDuration,
                });
                AddComponent(entity, new GeneralBuffData
                {
                    StartTime = 0,
                    TrackTarget = Entity.Null,
                    Duration = float.MaxValue
                });
            }
        }
    }

    public struct AoeInteractData : IComponentData
    {
        // Need to set data
        public float TriggerTime;
        public FactionTag TargetFaction;
        public StatChangeRequest StatChangeRequest;
        
        // Internal dynamic data
        public float CurrentTriggerCount;


        // Init data
        public float TriggerDuration;
        public float MaxTriggerCount;
        
    }

    
}