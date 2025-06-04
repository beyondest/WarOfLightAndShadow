using Unity.Entities;
using Unity.Mathematics;

namespace SparFlame.GamePlaySystem.General
{



  
    /// <summary>
    /// Units, Buildings, Env, Others
    /// </summary>
    public enum BaseTag
    {
        Units,
        Buildings,
        Resources
    }

    public enum FactionTag
    {
        Neutral = 0,
        Ally = 1,
        Enemy = ~1,
    }

    
    public struct GeneralAttr : IComponentData
    {
        public BaseTag BaseTag;
        public FactionTag FactionTag;
        public float3 BoxColliderSize;
        public int ID;
    }
    
    public struct AITag : IComponentData
    {
        
    }

    public struct PlayerTag : IComponentData
    {
        
    }
    public struct InTeamTag : IComponentData
    {
        public Entity BelongsToTeam;
    }

    /// <summary>
    /// This tagged entity must be destroyed when game over
    /// </summary>
    public struct GameplayEntityTag : IComponentData
    {
        
    }
    
    public struct EnemyCrystalInfo : IComponentData
    {
        public int TotalCount;
        /// <summary>
        /// This only counts in sight crystal, and if enemy is light, only count single crystal
        /// </summary>
        public int InSightValidCount;
        public float CurTotalHp;
        public float MaxTotalHp;
    }
    public struct PlayerCrystalInfo : IComponentData
    {
        public int TotalCount;
        public float CurTotalHp;
        public float MaxTotalHp;
    }
    
    
    /// <summary>
    /// This request is handled by stat system
    /// AbsAmount is always positive
    /// If Upgrade, kill by unnormal must be true
    /// </summary>
    public struct StatChangeRequest : IComponentData
    {
        public Entity Interactor;
        public Entity Interactee;
        public int AbsAmount;
        public StatChangeType Type;
        public GeneralAttr InteractorGeneralAttr;
    }

    public enum StatChangeType
    {
        None = 0,
        Attack = 1,
        Heal = 2,
        Harvest = 3,
        UnNormalKill = 4,
        SimpleCleanUsedAsUpgrade = 5
    }
    
    public enum Tier
    {
        Tier1 = 3,
        Tier2 = 4,
        Tier3 = 5,
        // Tier4 = 6,
        // Tier5 = 7,
    }
    
    public struct ExpData : IComponentData
    {
        public Tier MaxTier;
        public Tier CurTier;
        public int MaxValue;
        public float CurValue;
        public Entity NextTierPrefab;
    }

    public struct UnitDeadTag : IComponentData
    {
    }

    public enum InteractState
    {
        Idle = 0,
        Attacking = 1,
        Moving = 2,
        Garrison = 3,
        Harvesting =4,
        Healing = 5,
    }

    public struct BasicStateData : IComponentData
    {
        public InteractState CurState;
        public bool Focus;
        public Entity TargetEntity;
        public InteractState TargetState;
        public int InteractCounter;
    }
    public struct CameraData : IComponentData
    {
        public float4x4 ViewMatrix;
        public float4x4 ProjectionMatrix;
        public float2 ScreenSize;
        public float3 CameraRight;
        public float3 CameraForward;
        public float3 CameraUp;
        public float3 CameraRigPosition;
    }

}
