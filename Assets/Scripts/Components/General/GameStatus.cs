using SparFlame.Components.MainGameplay;
using Unity.Entities;

namespace SparFlame.Components.General
{
    public enum GameStatus
    {
        NotStarted = 0, // Stay in main menu and no resource loaded
        Init = 1, // When all resource loaded, but systems not init
        SubGaming = 2, // Gaming
        Pause = 3, // Gaming pause
        MainGaming = 4,
    }

    public enum SubGameStatus
    {
        None = 0,
        PlayerCity = 1,
        PlayerSiege = 3,
        PlayerDefend = 4,
        Encounter = 5,
        Support = 6,
    }

    public struct GameStatusData : IComponentData
    {
        public GameStatus Value;
    }

    public struct SubGameStatusData : IComponentData
    {
        public SubGameStatus SubGameStatus;
        public Entity City;
        public BattleTriggerRequest BattleTriggerRequest;
        public bool IsInBattle;
    }
    
    public struct InSubGameTag : IComponentData{}
}