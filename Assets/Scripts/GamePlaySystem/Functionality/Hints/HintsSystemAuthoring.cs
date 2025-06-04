using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Hints
{
    public class HintsSystemAuthoring : MonoBehaviour
    {
        private class HIntsSystemAuthoringBaker : Unity.Entities.Baker<HintsSystemAuthoring>
        {
            public override void Bake(HintsSystemAuthoring authoring)
            {
            }
        }
    }


    public enum HintName
    {
        None = 0,
        ResourceTierNotMatch = 1,
        ResourceAmountNotEnough = 2,
        ConstructOverlapping = 3,
        ConstructOnNotConstructable = 4,
        CrystalUnderAttack = 5,
        
        EnemyIsAssemblingAttackTeam = 6,
        EnemyIsAssemblingDefenseTeam = 7,
        EnemyIsAssemblingGatheringTeam = 8,
        EnemyIsAssemblingStrikeTeam = 9,
        
        NotEnoughResource = 10,
        CrystalCannotRelocate = 11,
        CannotRelocateWhenUnderAttack = 12,
        CannotRelocateWhenHasGarrisonUnits = 13,
        CannotRelocateWhenConstructing = 14,
        CrystalCannotRecycle = 15,
        CannotRecycleWhenUnderAttack = 16,
        CannotRecycleWhenConstructing = 17,
        
        TargetNotGarrisonable = 18,
    }

    public enum HintType
    {
        EnemyInfo = 0,
        EnemyWarning = 1,
        EnemyCritical = 2,
        PlayerInfo = 3,
        PlayerWarning = 4,
        PlayerCritical = 5,
    }

    public struct HintRequest : IComponentData
    {
        public float3 Position;
        public HintName Name;
    }

    public struct HintsInfo : IBufferElementData
    {
        public FixedString64Bytes Content;
        public float UpdateTime;
        public HintType HintType;
    }

    public struct HintConfigs : IBufferElementData
    {
        public HintName Name;
        public HintType Type;
        public FixedString64Bytes Content;
    }
    
}