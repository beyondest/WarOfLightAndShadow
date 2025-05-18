using System;
using SparFlame.GamePlaySystem.General;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SparFlame.GamePlaySystem.CustomParticleSystem
{
    public class CustomParticleSystemAuthoring : MonoBehaviour
    {
        public CParticleSystemConfig config;
        private class CustomParticleSystemAuthoringBaker : Baker<CustomParticleSystemAuthoring>
        {
            public override void Bake(CustomParticleSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, authoring.config);
            }
        }
    }

    [Serializable]
    public struct CParticleSystemConfig : IComponentData
    {
        public float killAliveScale;
    }
    
    
    public enum VFXName
    {
        None = 0,
        Upgrade = 1,
        ControlToAttack = 2,
        ControlToMarch = 3,
        TowerProjectile = 4,
        MagicSwordProjectile1 = 5,
        RangedUnitProjectile = 6,
        MagicShield = 7,
        FlameSpear1 = 8,
        SelectionIndicator = 9,
        MagicSwordProjectile2 = 10,
        MagicSwordProjectile3 = 11,
        ClericHealField = 12,
        FlameSpear2 = 13,
        FlameSpear3 = 14,
        ControlToGarrison = 15,
        ControlToHarvest = 16,
        ControlToHeal = 17
        
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
    
    
    public struct VFXRequest : IComponentData
    {
        /// <summary>
        /// If this vfx need to follow someone, this should not be Entity.Null
        /// </summary>
        public Entity VFXTrackTarget;

        public VFXRequestType Type;
        
        /// <summary>
        /// Spawn Position
        /// </summary>
        public float3 SpawnPosition;
        
        public float3 TargetPosition;
        
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

    public struct VFXData : IComponentData
    {
        public VFXType VFXType;
        public float StartTime;
        public float KeepDuration;
        public float TimeToLive;
        public Entity Tracker;
        public bool Reset;
        /// <summary>
        /// Only continuos vfx need this config 
        /// </summary>
        public bool KillUntilAllStopPlay;
        public float MaxWaitTimeForAllStopPlay;
    }
    
    
    public struct LateDestroyVFXTag : IComponentData
    {
        public float DestroyTime;
    }
    
    
    // public struct VFXPrefabPairComparer : IComparer<VFXPrefabDataPair>
    // {
    //     public int Compare(VFXPrefabDataPair x, VFXPrefabDataPair y)
    //     {
    //         return x.SubIndex.CompareTo(y.SubIndex);
    //     }
    // }

    public struct ProjectileInitData : IComponentData
    {
        public VFXName HitEffectName;
        public float BaseRelativeHeight;
        public float HorizontalSpeed;
        public float MaxFlightDistance;
        public bool NotStopUntilReachMaxDis;
        public bool IsParabola;
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
    public struct VFXRootTag : IComponentData
    {
        
    }
}
