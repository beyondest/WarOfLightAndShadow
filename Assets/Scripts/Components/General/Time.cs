using System;
using Unity.Entities;

namespace SparFlame.Components.General
{
    public struct GameTimeData : IComponentData
    {
        public float DeltaTime;
        public float ElapsedTime;
    }

    public struct GameTimeScale : IComponentData
    {
        public float Value;
    }



    [Serializable]
    public struct WorldTimeData : IComponentData
    {
        public float hour;
        public int day;
        public int month;
        public int year;
        public float deltaHour;
    }
    
}