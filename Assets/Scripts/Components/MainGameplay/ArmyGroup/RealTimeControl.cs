using System;
using Unity.Entities;
using Unity.Mathematics;

namespace SparFlame.Components.MainGameplay
{
    [Serializable]
    public struct ArmyGroupSkillConfig : IComponentData
    {
    }
    public struct ArmyGroupSprintRequest : IComponentData
    {
        public Entity ArmyGroup;
    }

    public struct ArmyGroupHoldSwitchRequest : IComponentData
    {
        public Entity ArmyGroup;
    }


    public struct ArmyGroupHoldOnTag : IComponentData
    {
        
    }
    public struct HoldOnPosition : IComponentData
    {
        public float3 Position;
    }

    public struct ArmyGroupSkillTimer : IComponentData
    {
        public float SprintCoolDown;
        public float ChargeCoolDown;
        public float MaxChargeCoolDown;
        public float MaxSprintCoolDown;
    }
}