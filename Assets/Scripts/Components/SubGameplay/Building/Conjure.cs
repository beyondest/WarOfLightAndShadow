using Unity.Entities;
using Unity.Mathematics;

namespace SparFlame.Components.SubGameplay
{
    public struct ConjureAttr : IComponentData
    {
        public UnitType ConjuringType;
        public float3 ConjurePositionBias;
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
        public int TargetAmount;
        public int ConjuredAmount;
        public float RemainingTimeHours;
        public float AccumulatedHours;
    }

}