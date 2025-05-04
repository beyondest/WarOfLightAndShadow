using System;
using System.Collections.Generic;
using SparFlame.GamePlaySystem.Map.GamePlaySystem.Core.Map;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Serialization;

namespace SparFlame.GamePlaySystem.RandomSpawn.GamePlaySystem.Functionality.RandomSpawn
{
    public class EnvSpawnSystemAuthoring : MonoBehaviour
    {
        public List<EnvSpawnTypeTotalAmount> envSpawnTypeTotalAmount;
        public List<EnvTileTypeSpecialData> envSpawnTypeSpecialDatas;
        private class EnvSpawnSystemAuthoringBaker : Baker<EnvSpawnSystemAuthoring>
        {
            public override void Bake(EnvSpawnSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                var buffer = AddBuffer<EnvSpawnTypeTotalAmount>(entity);
                for (var i = 0; i < authoring.envSpawnTypeTotalAmount.Count; i++)
                {
                    var data = authoring.envSpawnTypeTotalAmount[i];
                    if ((int)data.type != i)
                        throw new ArgumentException(
                            "Env Spawn Config wrong, env spawn data must follow the enum type sequence");
                    buffer.Add(new EnvSpawnTypeTotalAmount
                    {
                        amount = data.amount,
                        type = data.type
                    });
                }

                var buffer2 = AddBuffer<EnvTileTypeSpecialData>(entity);
                for (var i = 0; i < authoring.envSpawnTypeSpecialDatas.Count; i++)
                {
                    var data = authoring.envSpawnTypeSpecialDatas[i];
                    if ((int)data.type != i)
                        throw new ArgumentException(
                            "Env Spawn Config wrong, env special data must follow the enum type sequence");
                    buffer2.Add(data);
                }
            }
        }
    }

    public enum EnvType
    {
        Rock = 0,
        Grass = 1,
        Mountains = 2,
    }

    public struct EnvSpawnSystemConfig : IComponentData
    {
        
    }
    

    [Serializable]
    public struct EnvSpawnTypeTotalAmount : IBufferElementData
    {
        public EnvType type;
        public int amount;
    }
    
    public struct EnvSpawnPrefabData : IBufferElementData
    {
        public EnvType Type;
        public float Prob;
        public int Amount;
        public Entity Prefab;
    }

    [Serializable]
    public struct EnvTileTypeSpecialData : IBufferElementData
    {
        public EnvType type;
        public FixedList32Bytes<TileTypeToSpawnWeight> spawnableTiles;
    }
    
    [Serializable]
    public struct TileTypeToSpawnWeight
    {
        public TileType tileType;
        public float weight;
    }
    
    
    
    
}