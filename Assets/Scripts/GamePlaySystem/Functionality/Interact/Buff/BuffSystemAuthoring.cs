using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using SparFlame.GamePlaySystem.General;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Interact
{
    public class BuffSystemAuthoring : MonoBehaviour
    {
        private class BuffSystemAuthoringBaker : Baker<BuffSystemAuthoring>
        {
            public override void Bake(BuffSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new BuffSystemConfig
                {
                });
                
            }
        }
    }


    public enum BuffType
    {
        None = 0,
        AoeInteract = 1,
        
    }

    public enum BuffName
    {
        None = 0,
        MagicSwordSplash = 1,
        ClericHealCircle = 2,
    }

    public struct BuffRequest : IComponentData
    {
        public BuffName Name;
        public BuffType BuffType;
        public Entity TrackTarget;
        public float3 SpawnPosition;
        public quaternion SpawnRotation;
        public bool IfBuffLifeHandledByGeneralBuffManageSystem;
        public float Duration;
        public BuffFilter Filter;
    }
    
    
    public struct BuffData : IComponentData
    {
        public Entity TrackTarget;
        public float StartTime;
        public float Duration;
    }

    public struct BuffPrefabDataPair : IBufferElementData
    {
        public Entity Prefab;
        public BuffName Name;
        public BuffType BuffType;
        public BuffFilter Filter;
    }

    [Serializable]
    public struct BuffFilter
    {
        public bool factionFilterEnabled;
        [ShowIf(nameof(factionFilterEnabled))]
        public FactionTag faction;
        public bool tierFilterEnabled;
        [ShowIf(nameof(tierFilterEnabled))]
        public Tier tier;
    }

    public struct BuffSystemConfig : IComponentData
    {
    }
    
    public struct StaticBuffAttr : IComponentData
    {
        public BuffType Type;
        public float RangeSq;
        public float LastSeconds;
    }
    
}