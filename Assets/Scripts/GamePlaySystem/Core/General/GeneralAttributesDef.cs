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

    
    public struct SubGameplayGeneralAttr : IComponentData
    {
        public BaseTag BaseTag;
        public FactionTag FactionTag;
        public float3 BoxColliderSize;
        public int ID;
    }
    

    public struct InTeamTag : IComponentData
    {
        public Entity BelongsToTeam;
    }

    /// <summary>
    /// This tagged entity must be destroyed when game over
    /// </summary>
    public struct SubGameplayEntityTag : IComponentData
    {
        
    }

    public struct MainGameplayEntityTag : IComponentData
    {
        
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
