using System;
using System.Collections.Generic;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Map.GamePlaySystem.Core.Map;
using SparFlame.GamePlaySystem.RandomSpawn.GamePlaySystem.Functionality.RandomSpawn;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
namespace SparFlame.GamePlaySystem.Resource
{
    public class ResourceSpawnSystemAuthoring : MonoBehaviour
    {

        public List<ResourceTileTypeSpecialData> resourceTileTypeSpawnDatas;
        private class ResourceSpawnSystemBaker : Baker<ResourceSpawnSystemAuthoring>
        {
            public override void Bake(ResourceSpawnSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new ResourceSpawnSystemConfig
                {
                });
                var buffer = AddBuffer<ResourceTileTypeSpecialData>(entity);
                for (var i = 0; i < authoring.resourceTileTypeSpawnDatas.Count; i++)
                {
                    var data = authoring.resourceTileTypeSpawnDatas[i];
                    if ((int)data.type != i)
                        throw new ArgumentException(
                            "Resource spawn config authoring init error, resource tile special datas list must " +
                            "follow the enum type sequence");
                    
                    buffer.Add(data);
                }

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

    public struct ResourceSpawnSystemConfig : IComponentData
    {
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
    
    
    [Serializable]
    public struct ResourceTileTypeSpecialData : IBufferElementData
    {
        public ResourceType type;
        public FixedList32Bytes<TileTypeToSpawnWeight> spawnableTiles;
    }
    

    
}