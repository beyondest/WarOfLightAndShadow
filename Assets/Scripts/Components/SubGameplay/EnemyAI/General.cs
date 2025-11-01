using System;
using SparFlame.Components.MainGameplay;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SparFlame.Components.SubGameplay
{
    public struct AIUnitCommandData : IComponentData,IEnableableComponent
    {
        public float3 TargetPosition;
        public bool Focus;
    }

    public struct ArmyGroupAIData : IComponentData
    {
        public float StartWaitSeconds;
        public int WaypointIndex;
        public int LastFormationWaypointIndex;
        public SubGameplayArmyGroupState State;
        public bool IsWaiting;
        public bool IsContacted;
        public bool SkillCasted;
    }
    [Serializable]
    public struct SubGameplayArmyGroupWaypointData : IBufferElementData, IComparable<SubGameplayArmyGroupWaypointData>
    {
        [NonSerialized]
        public float3 SelfPosition;
        [Tooltip("What formation direction will be")]
        public float2 direction;
        [Tooltip("Wait time on pre waypoint")]
        public float waitBeforeMovingToThisWaypoint;
        public int index;
        public ArmyGroupIconType iconType;
        [Tooltip("Only valid when formation is enabled")]
        public FormationShape shape;
        [Tooltip("Only valid when formation skill > 0.You can only force cast skill one time for all the waypoints for an army group")]
        public bool forceCastSkillBeforeReachThisWaypoint;
        [Tooltip("Only valid when waitBeforeMovingToThisWaypoint is not 0")]
        public bool formationBeforeReachThisWaypoint;
   

        public int CompareTo(SubGameplayArmyGroupWaypointData other)
        {
            return index.CompareTo(other.index);
        }
    }
    public enum SubGameplayArmyGroupState
    {
        Idle = 0,
        Moving = 1,
        Interacting = 2,
        CastSkill = 3,
    }

 
}

