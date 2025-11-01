/*using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using SparFlame.Components.SubGameplay;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.Waves
{
    public class WaveSystemAuthoring : MonoBehaviour
    {
        [Header("Wave Point Interval Config"),TableList]
        public List<WavePointIntervalDataInspector> pairs;
        private class WaveSystemAuthoringBaker : Baker<WaveSystemAuthoring>
        {
            public override void Bake(WaveSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                var buffer = AddBuffer<WavePointToIntervalData>(entity);
                foreach (var pair in authoring.pairs)
                {
                    buffer.Add(new WavePointToIntervalData
                    {
                        Points = pair.waveCount,
                        Value = pair.intervalSeconds
                    });
                }
                AddComponent(entity, new GameWaveData
                {
                    CurWaveIndex = -1,
                    IfWaveUpdateThisFrame = false,
                    NextWaveRemainingTime = 0f
                });
                AddComponent(entity, new GameWaveSystemConfig());
            }
        }
    }


    [Serializable]
    public struct WavePointIntervalDataInspector
    {
        public int waveCount;
        public int intervalSeconds;
    }
    
  
    public struct GameWaveSystemConfig : IComponentData
    {
        
    }
 



    
}*/