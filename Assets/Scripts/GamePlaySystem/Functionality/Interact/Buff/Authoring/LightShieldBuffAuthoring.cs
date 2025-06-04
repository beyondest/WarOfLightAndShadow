using Sirenix.OdinInspector;
using SparFlame.GamePlaySystem.EnemyAI;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Serialization;

namespace SparFlame.GamePlaySystem.Interact.ShieldDefense
{
    public class LightShieldBuffAuthoring : MonoBehaviour
    {
        [AssetsOnly] public GameObject aoeTriggerPrefab;
        public float getDamageScale;
        public float selfGetDamageScale;
        
        [Tooltip("Affect how soon it will check to add under defend buff, as well as check to remove out of range light shield buff")]
        public float checkDuration;
        
        private class ShieldBuffAuthoringBaker : Baker<LightShieldBuffAuthoring>
        {
            public override void Bake(LightShieldBuffAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<AoeTarget>(entity);
                AddComponent(entity, new AoeTriggerRequest
                {
                    Prefab = GetEntity(authoring.aoeTriggerPrefab, TransformUsageFlags.Dynamic)
                });
                
                AddComponent(entity, new LightShieldBuffData
                {
                    Defender = Entity.Null,
                    GetDamageScale = authoring.getDamageScale,
                    SelfGetDamageScale = authoring.selfGetDamageScale,
                    CheckDuration = authoring.checkDuration,
                    TriggerTime = 0,
                });
                AddComponent(entity, new GeneralBuffData
                {
                    StartTime = 0,
                    TrackTarget = Entity.Null,
                    Duration = float.MaxValue
                });
                AddBuffer<PreviousAoeTarget>(entity);
                
            }
        }
    }

    public struct LightShieldBuffData : IComponentData
    {
        public Entity Defender;

        public float GetDamageScale;
        public float SelfGetDamageScale;
        
        public float TriggerTime;
        public float CheckDuration;
    }


    public struct LightShieldDefenderData : IBufferElementData
    {
        public Entity Entity;
        public float GetDamageScale;
        public float SelfGetDamageScale;
        // Tier3 shield will make ally not hurt from aoe, until itself dead
        public bool IfTier3;
    }

    public struct PreviousAoeTarget : IBufferElementData
    {
        public Entity Entity;
    }
    
}