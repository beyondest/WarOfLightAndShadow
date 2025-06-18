using Unity.Entities;

namespace SparFlame.Components.MainGameplay
{
    public enum BattleFieldType
    {
        None = 0,
        Prairie = 1,
        Forest = 2,
    }
    
    public enum BattleType
    {
        Encounter,
        Siege,
    }
    
    public struct BattleTriggerRequest : IComponentData
    {
        public Entity Attacker;
        public Entity Defender;
        public BattleType Type;
    }
}