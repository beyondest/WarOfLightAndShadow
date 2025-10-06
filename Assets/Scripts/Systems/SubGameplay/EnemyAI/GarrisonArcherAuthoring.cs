using Sirenix.OdinInspector;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.SubGameplay.EnemyAI
{
    public class GarrisonArcherAuthoring : MonoBehaviour
    {
        [SceneObjectsOnly] public GameObject targetWall;
        private class GarrisonArcherAuthoringBaker : Baker<GarrisonArcherAuthoring>
        {
            public override void Bake(GarrisonArcherAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                var wallEntity = GetEntity(authoring.targetWall, TransformUsageFlags.Dynamic);
                AddComponent(entity, new EnemyGarrisonArcher { TargetWall = wallEntity });
            }
        }
    }

    public struct EnemyGarrisonArcher : IComponentData
    {
        public Entity TargetWall;
    }
}