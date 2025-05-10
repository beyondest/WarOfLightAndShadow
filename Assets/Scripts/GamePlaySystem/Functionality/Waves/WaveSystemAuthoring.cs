using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using SparFlame.GamePlaySystem.General;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Waves
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
                    NeedUpdateWaveTime = -1
                });
            }
        }
    }


    [Serializable]
    public struct WavePointIntervalDataInspector
    {
        public int waveCount;
        public int intervalSeconds;
    }
    
    public struct WavePointToIntervalData : IBufferElementData,IPointsData<int>
    {
        // Wave count
        public int Points { get; set; }
        // Value is interval seconds
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