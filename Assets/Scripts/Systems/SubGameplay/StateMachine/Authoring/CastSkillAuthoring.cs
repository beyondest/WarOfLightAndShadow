using SparFlame.Components.SubGameplay;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.SubGameplay.StateMachine
{
    public class CastSkillAuthoring : MonoBehaviour
    {
        [Header("General")]
        public string endEventName = "End";
        
        [Header("Cleric Skill")]
        public float clericSkillAmountScale = 2f;
        public float clericAoeTriggerDelay = 0.5f;
        public float clericSkillLastDuration = 4f;
        
        [Header("Spell Sword Skill")]
        public float spellSwordSkillAmountScale = 0.5f;
        public float spellSwordSkillTriggerDelay = 0.5f;
        public float spellSwordSkillLastDuration = 4f;
        
        [Header("Archer Skill")]
        public float rotationSpeedWhileAiming = 8f;
        public float arrowRainRadius = 0.5f;
        public float arrowSpeed = 10f;
        
        private class CastSkillAuthoringBaker : Baker<CastSkillAuthoring>
        {
            public override void Bake(CastSkillAuthoring authoring)
            {
                var e = GetEntity(TransformUsageFlags.None);
                FixedString64Bytes n = authoring.endEventName;
                AddComponent(e, new CastSkillConfig
                {
                    EndEventNameHash = n.GetHashCode(),
                    SpellSwordSkillAmountScale = authoring.spellSwordSkillAmountScale,
                    ClericSkillAmountScale = authoring.clericSkillAmountScale,
                    ClericAoeTriggerDelay = authoring.clericAoeTriggerDelay,
                    SpellSwordSkillTriggerDelay = authoring.spellSwordSkillTriggerDelay,
                    ClericSkillLastDuration = authoring.clericSkillLastDuration,
                    SpellSwordSkillLastDuration = authoring.spellSwordSkillLastDuration
                });
                AddComponent(e, new ArcherSkillConfig
                {
                    ArrowSpeed = authoring.arrowSpeed,
                    ArrowRainRadius = authoring.arrowRainRadius,
                    RotationSpeedWhileAiming = authoring.rotationSpeedWhileAiming
                });
                AddComponent(e, new PlayerArcherSkill());
                AddComponent(e, new EnemyArcherSkill());
            }
        }
    }

    public struct CastSkillConfig : IComponentData
    {
        public int EndEventNameHash;
        public float ClericSkillAmountScale;
        public float ClericAoeTriggerDelay;
        public float ClericSkillLastDuration;
        
        public float SpellSwordSkillAmountScale;
        public float SpellSwordSkillTriggerDelay;
        public float SpellSwordSkillLastDuration;
    }

    public struct ArcherSkillConfig : IComponentData
    {
        public float RotationSpeedWhileAiming;
        public float ArrowRainRadius;
        public float ArrowSpeed; // This value should be same as arrow projectile config
    }
    
    
    
}