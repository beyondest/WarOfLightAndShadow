using Unity.Entities;
using Unity.Transforms;

namespace SparFlame.Components.MainGameplay
{
    public struct BeforeBattleArmyGroupSnapShot : IComponentData
    {
        public int UnitCount;
        public float MaxHp;
    }

    public struct ArmyGroupStatChangeRequest : IComponentData
    {
        public Entity ArmyGroup;
        public int StatChangeValue;
    }

    public struct BeforeBattleTotalSnapShot : IComponentData
    {
        public int PlayerSideUnitCount;
        public int EnemySideUnitCount;
    }
}