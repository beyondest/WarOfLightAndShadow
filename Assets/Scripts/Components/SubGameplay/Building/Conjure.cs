using Unity.Entities;
using Unity.Mathematics;

namespace SparFlame.Components.SubGameplay
{
    public struct ConjureAttr : IComponentData
    {
        public float3 ConjurePositionBias;
        public UnitType ConjuringType;
    }
    public struct ConjureRequest : IComponentData
    {
        public Entity UnitPrefab;
        public Entity BuildingEntity;
        public int Count;
    }

    public struct ConjuringTag : IComponentData
    {
        
    }

    
    public struct ConjuringData : IBufferElementData
    {
        public Entity ConjuringEntity;
        public float ThisTaskRemainingTime;
        public float LastCheckTotalHours;
        public int PrefabId;
        public int TargetAmount;
        public int ConjuredAmount;
    }

}