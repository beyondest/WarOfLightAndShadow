using Unity.Entities;
using Unity.Mathematics;

namespace SparFlame.Components.General
{
    
    public enum AudioName
    {
        None = 0,
        ErrorAction = 1,
        LightShield = 2,
        DarkShield = 3,
        Spear = 4,
        LightDead = 5,
        DarkDead = 6,
        LightFlameBurn = 7,
        DarkFlameBurn = 8,
        MagicSwordSplash = 9,
        ClericHealCircle = 10,
        ArrowShoot = 11,
        TowerMagicBallStart = 12,
        TowerMagicCircle = 13,
        TowerMagicBallHit = 14,
        BuildingDestroyed = 15,
        WorkerHarvest = 16,
        Construct = 17,
        NextTier = 18,
        Recycle = 19,
        UnitUpgrade = 20,
        ArmyGroupStartMoving = 21,
    }
    
    public struct AudioRequest : IComponentData
    {
        public AudioName Name;
        public float3 Position;
    }
}