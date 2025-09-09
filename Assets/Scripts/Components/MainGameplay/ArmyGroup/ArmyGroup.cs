using System;
using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using SparFlame.Core.Utils;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SparFlame.Components.MainGameplay
{
    


    // Army Group Attr

    public enum ArmyGroupIconType
    {
        Bear,
        Butterfly,
        Dragon,
        Deer,
        Horse,
        Lion,
        Rabbit,
        Scorpion,
        Snake,
        Wolf
    }
    
    [Serializable]
    public struct ArmyGroupAttr : IComponentData
    {
        // Static data
        public ArmyGroupIconType iconType;
        public FixedString32Bytes gameplayName;
        public long saveId;        // This id is generated when new an army group, and will never duplicate nor change.
        public float createTimeInTotalHours;
        
        // Unit data
        public int avgLevel;
        public int tier1UnitCount;
        public int tier2UnitCount;
        public int tier3UnitCount;
        
        // This is used to record units formation info
        public float3 boundingBoxMin;
        public float3 boundingBoxMax;
        public float3 loadingCenter;
        public float3 loadingScale;

    }

    [Serializable]
    public struct ArmyGroupStatData : IComponentData
    {
        public float totalMaxHp;
        public float totalCurrentHp;
        public float recoveredHpRatio;
        public int lastCheckTotalHours; // Used for hp auto recovery, reset to current total hours immediately when army group is in garrison state
    }



    public struct LastPassingByPlayerCity : IComponentData
    {
        public Entity City;
    }




    public struct ArmyGroupUtils
    {
        /// <summary>
        /// This function can only be called in sub gameplay, use it to update army group info when composition may be changed.
        /// </summary>
        /// <param name="em"></param>
        /// <param name="armyGroup"></param>
        public static void UpdateArmyGroupInfoForCompoChanged(EntityManager em, Entity armyGroup)
        {
            var armyGroupAttr = em.GetComponentData<ArmyGroupAttr>(armyGroup);
            var armyGroupMovableData = em.GetComponentData<ArmyGroupMovableData>(armyGroup);
            var armyGroupUnits = em.GetBuffer<ArmyGroupUnit>(armyGroup);
            var armyGroupStatData= em.GetComponentData<ArmyGroupStatData>(armyGroup);
            
            
            var unitCount = armyGroupUnits.Length;
            var totalLevel = 0;
            var totalTier1Count = 0;
            var totalTier2Count = 0;
            var totalTier3Count = 0;
            var minSpeed = float.MaxValue;
            var currentHp = 0f;
            var maxHp = 0f;

          

            
            for (var i = 0; i < unitCount; i++)
            {
                var unit = armyGroupUnits[i].Unit;
                var expData = em.GetComponentData<ExpData>(unit);
                var movableData = em.GetComponentData<MovableData>(unit);
                var statData = em.GetComponentData<StatData>(unit);
                if(movableData.MoveSpeed < minSpeed) minSpeed = movableData.MoveSpeed;
                totalLevel += expData.curLevel;
                currentHp += statData.curValue;
                maxHp += statData.maxValue;
                
                
                switch (expData.curTier)
                {
                    case Tier.Tier1:
                        totalTier1Count++;
                        break;
                    case Tier.Tier2:
                        totalTier2Count++;
                        break;
                    case Tier.Tier3:
                        totalTier3Count++;
                        break;
                    default:
                        BurstSafe.UnexpectedEnum(expData.curTier);
                        break;
                }
            }
            
            armyGroupAttr.avgLevel = unitCount == 0? 0 : totalLevel / unitCount;
            armyGroupMovableData.minUnitMoveSpeed = unitCount == 0? 0 : minSpeed;
            
            
            armyGroupAttr.tier1UnitCount = totalTier1Count;
            armyGroupAttr.tier2UnitCount = totalTier2Count;
            armyGroupAttr.tier3UnitCount = totalTier3Count;
            
            
            armyGroupStatData.totalCurrentHp = currentHp;
            armyGroupStatData.totalMaxHp = maxHp;
            
            em.SetComponentData(armyGroup, armyGroupAttr);
            em.SetComponentData(armyGroup, armyGroupMovableData);
            em.SetComponentData(armyGroup, armyGroupStatData);
            
        }
    }

    

    
  
    
}