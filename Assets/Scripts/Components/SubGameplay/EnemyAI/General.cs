using Unity.Entities;
using Unity.Mathematics;

namespace SparFlame.Components.SubGameplay
{
    public struct AIUnitCommandData : IComponentData,IEnableableComponent
    {
        public Entity Target;
        public float3 TargetPosition;
        public InteractState TargetState;
        public bool Focus;
    }
}

