using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;


namespace SparFlame.System.Spawn
{
    public class SpawnSystemAuthoring : MonoBehaviour
    {

        public Transform spawnPos;
        public GameObject spawnPrefab;
        public KeyCode spawnKey = KeyCode.Return;

        class Baker : Baker<SpawnSystemAuthoring>
        {

            public override void Bake(SpawnSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new SpawnSystemData
                {
                    SpawnPos = authoring.spawnPos.position,
                }); 
                AddComponent(entity, new SpawnSystemConfig
                {
                    SpawnKey = authoring.spawnKey,
                    SpawnPrefab = GetEntity(authoring.spawnPrefab, TransformUsageFlags.Dynamic)
                });
            }
        }
    }


    public struct SpawnSystemConfig : IComponentData
    {
        public KeyCode SpawnKey;
        public Entity SpawnPrefab;

    }

    public struct SpawnSystemData : IComponentData
    {
        public float3 SpawnPos;
    }
}