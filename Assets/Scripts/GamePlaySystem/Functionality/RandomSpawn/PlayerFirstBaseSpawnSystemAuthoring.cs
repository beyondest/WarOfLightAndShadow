using Sirenix.OdinInspector;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SparFlame.GamePlaySystem.RandomSpawn.GamePlaySystem.Functionality.RandomSpawn
{
    public class PlayerFirstBaseSpawnSystemAuthoring : MonoBehaviour
    {
        [AssetsOnly]
        public GameObject darkCrystalPrefab;
        [AssetsOnly]
        public GameObject lightCrystalPrefab;
        private class Baker : Baker<PlayerFirstBaseSpawnSystemAuthoring>
        {
            public override void Bake(PlayerFirstBaseSpawnSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new PlayerFirstBaseEntity
                {
                    DarkPrefab = GetEntity(authoring.darkCrystalPrefab, TransformUsageFlags.Dynamic),
                    LightPrefab = GetEntity(authoring.lightCrystalPrefab, TransformUsageFlags.Dynamic),
                });
            }
        }
    }

 

    public struct PlayerFirstBaseEntity : IComponentData
    {
        public Entity LightPrefab;
        public Entity DarkPrefab;
    }
}