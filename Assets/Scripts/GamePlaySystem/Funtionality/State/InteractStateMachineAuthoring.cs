using Unity.Entities;
using UnityEngine;
using UnityEngine.Serialization;

namespace SparFlame.GamePlaySystem.State
{
    public class InteractStateMachineAuthoring : MonoBehaviour
    {

        [Header("Internal Config")]
        public int attackJobBatchCount = 16;
        public int healJobBatchCount = 16;
        public int harvestJobBatchCount = 16;
        public float interactTurnSpeed = 7.5f;

        
        private class InteractStateMachineAuthoringBaker : Baker<InteractStateMachineAuthoring>
        {
            public override void Bake(InteractStateMachineAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new InteractStateMachineConfig
                {
                    AttackJobBatchCount = authoring.attackJobBatchCount,
                    HealJobBatchCount = authoring.healJobBatchCount,
                    HarvestJobBatchCount = authoring.harvestJobBatchCount,
                    InteractTurnSpeed = authoring.interactTurnSpeed,
                    
                });

            }
        }
    }

    public struct InteractStateMachineConfig : IComponentData
    {
        public int AttackJobBatchCount;
        public int HealJobBatchCount;
        public int HarvestJobBatchCount;
        public float InteractTurnSpeed;
    }


}