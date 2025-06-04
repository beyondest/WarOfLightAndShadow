using SparFlame.GamePlaySystem.Units;
using Unity.Entities;
using Unity.Mathematics;

namespace SparFlame.GamePlaySystem.Conjure
{
    // public class ConjureAttributesAuthoring : MonoBehaviour
    // {
    //     public UnitType conjuringType;
    //     private class ConjuringAttributesAuthoringBaker : Baker<ConjureAttributesAuthoring>
    //     {
    //         public override void Bake(ConjureAttributesAuthoring authoring)
    //         {
    //             var entity = GetEntity(TransformUsageFlags.Dynamic);
    //             AddComponent(entity, new ConjureAttr
    //             {
    //                 ConjuringType = authoring.conjuringType
    //             });
    //             AddBuffer<ConjuringData>(entity);
    //         }
    //     }
    // }

    public struct ConjureAttr : IComponentData
    {
        public UnitType ConjuringType;
        public float3 ConjurePositionBias;
    }

    public struct ConjuringData : IBufferElementData
    {
        public Entity ConjuringEntity;
        public int TargetAmount;
        public int ConjuredAmount;
        public float RemainingTimeSeconds;
        public float Counter;
    }
    
    public struct EnemyConjureShrineData : IComponentData
    {
        public Random Rnd;
        public Entity Base;
        public float ConjureTime;
    }
    
}