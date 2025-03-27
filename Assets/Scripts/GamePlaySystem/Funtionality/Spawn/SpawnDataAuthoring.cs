using UnityEngine;
using Unity.Entities;


namespace SparFlame.GamePlaySystem.Spawn
{
    public class SpawnDataAuthoring : MonoBehaviour
    {

        public Transform spawnPos;
        public GameObject spawnPrefab;
        public KeyCode spawnKey = KeyCode.Return;

        class Baker : Baker<SpawnDataAuthoring>
        {
            public override void Bake(SpawnDataAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new SpawnedData()
                {
                    SpawnPos = authoring.spawnPos == null ? authoring.spawnPos.position : Vector3.zero,
                    SpawnPrefab = GetEntity(authoring.spawnPrefab, TransformUsageFlags.Dynamic),
                    SpawnKey = authoring.spawnKey
                }); 
                AddComponent<Spawnable>(entity);
                SetComponentEnabled<Spawnable>(entity, true);
            }
        }
    }
}