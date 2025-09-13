using Unity.Entities;

namespace SparFlame.Components.MainGameplay
{
    public enum ArmyGroupState
    {
        Idle,
        Moving,
        Invade,
        Support,
        Garrison,
        Station, 
    }

    public struct ArmyGroupStateData : IComponentData
    {
        public Entity Target;
        public ArmyGroupState CurState;
        public ArmyGroupState TargetState;
    }
    
    public struct ArmyGroupSightTarget : IBufferElementData
    {
        public Entity Entity;
    }
}