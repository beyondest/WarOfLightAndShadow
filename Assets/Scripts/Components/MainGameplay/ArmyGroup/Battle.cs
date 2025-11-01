using Unity.Entities;
using Unity.Transforms;

namespace SparFlame.Components.MainGameplay
{
    public struct BeforeBattleArmyGroupSnapShot : IComponentData
    {
        public float MaxHp;
        public int UnitCount;
    }

    public struct ArmyGroupStatChangeRequest : IComponentData
    {
        public Entity ArmyGroup;
        public int StatChangeValue;
    }

    public struct BeforeBattleArmyGroupTotalSnapshot : IComponentData
    {
        public int PlayerSideUnitCount;
        public int EnemySideUnitCount;
    }
}