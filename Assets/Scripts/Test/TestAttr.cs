using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
namespace SparFlame.Test
{
    public class TestSpawnerAuthoring : MonoBehaviour
    {
        public Transform spawnPosition;
        public int entitiesPerFrame = 1;
        public float initialScale = 1;
        class TestSpawnerBaker : Baker<TestSpawnerAuthoring>
        {
            public override void Bake(TestSpawnerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new TestSpawner()
                {
                    EntitiesPerFrame = authoring.entitiesPerFrame,
                    SpawnPosition = authoring.spawnPosition.position,
                    InitialScale = authoring.initialScale,
                });
            }
        }
    }
    public struct TestSpawner : IComponentData
    {
        public float3 SpawnPosition;
        public int EntitiesPerFrame;
        public float InitialScale;
    }
}