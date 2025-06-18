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
}