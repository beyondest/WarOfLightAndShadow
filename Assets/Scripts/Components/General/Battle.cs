using System;
using System.ComponentModel;
using SparFlame.Components.MainGameplay;
using Unity.Entities;
using Unity.Mathematics;

namespace SparFlame.Components.General
{
    public enum BattleFieldType
    {
        None = 0,
        Prairie = 1,
        Forest = 2,
    }
    
    public struct BattleTriggerRequest : IComponentData
    {
        public Entity Attacker;
        public Entity Defender;
        public SubGameStatus TargetSubGameStatus;
    }
    public struct SupportFightTag : IComponentData{}

    public struct SupportFightInvader : IBufferElementData
    {
        public Entity Entity;
    }
    
    
    public struct BattleCheckSightTarget : IBufferElementData
    {
        public Entity Entity;
    }
    
    public struct BattleCheckSightDataBelongsTo : IComponentData
    {
        public Entity Value;
    }

    public struct BattleCheckSightConnectTo : IComponentData
    {
        public Entity Value;
    }
    
      
    [Serializable]
    public struct LoadingGridInfo: IBufferElementData
    {
        public float3 outerCenter;
        public float outerSize;
        public float3 innerCenter;
        public float innerSize;
    }


    public struct EcoEntityData : IBufferElementData
    {
        public EcoType EcoType;
        public Entity EcoEntity;
    }
}