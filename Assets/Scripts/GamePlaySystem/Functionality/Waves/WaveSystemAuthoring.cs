using System;
using System.Collections.Generic;
using System.ComponentModel;
using SparFlame.GamePlaySystem.General;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Serialization;

namespace SparFlame.GamePlaySystem.Waves
{
    public class WaveSystemAuthoring : MonoBehaviour
    {
        [Header("Wave Point Interval Config")]
        public List<WavePointToIntervalData> pairs;
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
                        Points = pair.Points,
                        Value = pair.Value
                    });
                }
            }
        }
    }

    [Serializable]
    public struct WavePointToIntervalData : IBufferElementData,IPointsData<int>
    {
        public int Points { get; set; }
        // Value is interval
        public int Value {get;set;}
    }
    
    public struct GameWaveSystemConfig : IComponentData
    {
        
    }
    
    public struct GameWaveData : IComponentData
    {
        public int CurWaveIndex;
        public float NeedUpdateWaveTime;
        public bool IfWaveUpdateThisFrame;
    }

    public struct NextWaveRequest : IComponentData
    {
        
    }
    
}