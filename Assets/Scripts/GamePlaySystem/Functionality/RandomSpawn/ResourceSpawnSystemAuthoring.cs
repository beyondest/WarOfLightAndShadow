using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.RandomSpawn;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
namespace SparFlame.GamePlaySystem.Resource
{
    public class ResourceSpawnSystemAuthoring : MonoBehaviour
    {
        public ResourceSpawnSystemConfig config;
        private class ResourceSpawnSystemBaker : Baker<ResourceSpawnSystemAuthoring>
        {
            public override void Bake(ResourceSpawnSystemAuthoring authoring)
            {
                
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, authoring.config);
                AddComponent(entity, new ResourceSpawnState
                {
                    LastTimePoint = -1
                });
                
            }
        }
    }

    public struct ResourcePointData : IPointsData<int>,IBufferElementData
    {
        public int Points { get; set; }

        public int Value { get => Points; set => Points = value; }
    }

    [Serializable]
    public struct ResourceSpawnSystemConfig : IComponentData
    {
        public float edgeMargin;
    }
    
    public struct ResourceSpawnData : IBufferElementData
    {
        public int TimePoints;
        public ResourceType ResourceType;
        public int Amount;
    }
    
    public struct ResourceSpawnState : IComponentData
    {
        public int LastTimePoint;
    }
    
    
    public struct ResourceTileTypeSpecialData : IBufferElementData
    {
        public ResourceType Type;
        public FixedList128Bytes<TileTypeToSpawnWeight> SpawnableTiles;
    }
    [Serializable]
    public class ResourceTypeSpawnDatabaseItem 
    {
        [HideLabel]
        [TableColumnWidth(100,false)]
        [VerticalGroup("Resource")]
        public ResourceType type;
        [TableList]
        public List<TileTypeToSpawnWeight> spawnableTiles;
    }
    

    
}