using SparFlame.Components.SubGameplay;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.SubGameplay.EnemyAI
{
    public class EnemyTeamStateMachineAuthoring : MonoBehaviour
    {
        public EnemyTeamStateMachineConfig config;
        private class EnemyTeamStateMachineAuthoringBaker : Baker<EnemyTeamStateMachineAuthoring>
        {
            public override void Bake(EnemyTeamStateMachineAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, authoring.config);
            }
        }
    }



}