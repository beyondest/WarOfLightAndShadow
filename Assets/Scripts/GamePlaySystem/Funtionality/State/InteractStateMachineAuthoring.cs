using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.State
{
    public class InteractStateMachineAuthoring : MonoBehaviour
    {

        public int attackJobBatchCount = 16;
        public int healJobBatchCount = 16;
        public int harvestJobBatchCount = 16;
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
                });
            }
        }
    }

    public struct InteractStateMachineConfig : IComponentData
    {
        public int AttackJobBatchCount;
        public int HealJobBatchCount;
        public int HarvestJobBatchCount;
    }
}