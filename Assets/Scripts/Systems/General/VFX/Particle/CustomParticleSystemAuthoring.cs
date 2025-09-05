using System;
using SparFlame.Components.VFX;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.General.VFX
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
    
    

   
}
