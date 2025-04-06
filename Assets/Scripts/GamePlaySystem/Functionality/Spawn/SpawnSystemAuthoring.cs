using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Spawn
{
    public class SpawnSystemAuthoring : MonoBehaviour
    {
        class Baker : Baker<SpawnSystemAuthoring>
        {
            public override void Bake(SpawnSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<SpawnSystemConfig>(entity);
            }
        }
    }

    public struct SpawnedData : IComponentData
    {
        public Vector3 SpawnPos;
        public Entity SpawnPrefab;
        public KeyCode SpawnKey;
    }

    public struct SpawnSystemConfig : IComponentData
    {
        
    }

    public struct Spawnable : IComponentData, IEnableableComponent
    {
        
    }
}