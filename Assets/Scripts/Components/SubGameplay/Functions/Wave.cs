using SparFlame.Core.Interfaces;
using Unity.Entities;

namespace SparFlame.Components.SubGameplay
{
    public struct WavePointToIntervalData : IBufferElementData,IPointsData<int>
    {
        // Wave count
        public int Points { get; set; }
        // Value is interval seconds
        public int Value {get;set;}
    }
       
    public struct NextWaveRequest : IComponentData
    {
        
    }
    public struct GameWaveData : IComponentData
    {
        public int CurWaveIndex;
        public float NextWaveRemainingTime;
        public float CurWaveInterval;
        public bool IfWaveUpdateThisFrame;
    }
}