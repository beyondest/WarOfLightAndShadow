using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using Unity.Entities;
using Unity.Mathematics;

namespace SparFlame.Components.VFX
{
    public enum VFXName
    {
        None = 0,
        NextTier = 1,
        ControlToAttack = 2,
        ControlToMarch = 3,
        TowerProjectile = 4,
        MagicSwordProjectile1 = 5,
        RangedUnitProjectile = 6,
        LightShield = 7,
        FlameSpear1 = 8,
        UnitSelectionIndicator = 9,
        MagicSwordProjectile2 = 10,
        MagicSwordProjectile3 = 11,
        ClericHealField = 12,
        FlameSpear2 = 13,
        FlameSpear3 = 14,
        ControlToGarrison = 15,
        ControlToHarvest = 16,
        ControlToHeal = 17,
        Construct = 18,
        ConjureUnit = 19,
        TowerHitEffect = 20,
        TowerCircleAttack = 21,
        CrystalBeaconAttack = 22,
        GarrisonUnderDefense = 23,
        GarrisonUnderDefenseTier4 = 24,
        DarkShield = 25,
        LightCavalryGain = 26,
        DarkCavalryAttackGain = 27,
        CavalryMoveDamageReduction = 28,
        DarkClericAttackGain = 29,
        LightMagicDamageDebuff = 30,
        DarkMagicDamageDebuff = 31,
        UnitUpgrade = 32,
        UnitGarrisonBuff = 33,
        ArmyGroupSelectionIndicator = 34,
        ControlToRetreat = 35,
    }

    public enum VFXType
    {
        None = 0,
        // Explosion, lightening
        Instant,
        // Buff
        Continuos,
        // Will send stat change request when reach target
        Projectile
    }

    public enum VFXRequestType
    {
        /// <summary>
        /// Kill type must have a tracker
        /// </summary>
        Kill = 0,
        Spawn = 1,
    }
    
    public enum ProjectileType
    {
        Parabola = 0,
        NoHeightChangeUntilReachMaxDis = 1,
        GoStraightToTargetWithHeightChange = 2,
    }
    public struct VFXRequest : IComponentData
    {
        /// <summary>
        /// If this vfx need to follow someone, this should not be Entity.Null
        /// </summary>
        public Entity VFXTrackTarget;

        public VFXRequestType RequestType;
        
        /// <summary>
        /// Spawn Position
        /// </summary>
        public float3 SpawnPosition;
        
        public float3 ParabolaTargetPosition;
        
        /// <summary>
        /// Specify which vfx prefab to use in prefab database 
        /// </summary>
        public VFXName VFXName;

        
        /// <summary>
        /// If 0, this vfx is either instantly (means it will be killed when play is stopped or projectile reach the target )
        /// or lives forever until manually killed
        /// </summary>
        public float KeepDuration;
        
        public VFXSubFilter Filter;
        
        /// <summary>
        /// If this is projectile
        /// </summary>
        public StatChangeRequest StatChangeRequest;
    }

    public struct VFXSubFilter
    {
        public bool FactionFilterEnable;
        public FactionTag Faction;
        public bool TierFilterEnable;
        public Tier Tier;
    }
    
    
    
    

    public struct ProjectileInitData : IComponentData
    {
        public VFXName HitEffectName;
        public float BaseRelativeHeight;
        public float HorizontalSpeed;
        public float MaxFlightDistance;
        public float InitialHeight;
        public ProjectileType ProjectileType;
    }

  
    
    public struct VFXPrefabDataPair
    {
        public Entity Prefab;
        public VFXType VFXType;
        public ProjectileInitData PData;
        public VFXSubFilter Filter;
        public bool KillUntilAllStopPlay;
        public float MaxWaitTimeForAllStopPlay;
    }
    
    public struct VFXConfigData : IBufferElementData
    {
        public VFXName Name;
        public VFXPrefabDataPair Pair;
    }

    public struct TrackedByVFX : IBufferElementData
    {
        public VFXName Name;
        public Entity VFX;
    }
}