using SparFlame.Components.SubGameplay;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.SubGameplay.EnemyAI
{
    public class ArmyGroupWaypointAuthoring : MonoBehaviour
    {
        public SubGameplayArmyGroupWaypointData data;
        private class ArmyGroupWaypointAuthoringBaker : Baker<ArmyGroupWaypointAuthoring>
        {
            public override void Bake(ArmyGroupWaypointAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                var buffer = AddBuffer<SubGameplayArmyGroupWaypointData>(entity);
                buffer.Add(authoring.data);
            }
        }
    }

   
}