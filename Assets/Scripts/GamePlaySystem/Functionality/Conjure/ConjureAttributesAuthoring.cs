using SparFlame.GamePlaySystem.Units;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Spawn
{
    public class ConjureAttributesAuthoring : MonoBehaviour
    {
        public UnitType conjuringType;
        private class ConjuringAttributesAuthoringBaker : Baker<ConjureAttributesAuthoring>
        {
            public override void Bake(ConjureAttributesAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new ConjureAttr
                {
                    ConjuringType = authoring.conjuringType
                });
                AddBuffer<ConjuringData>(entity);
            }
        }
    }

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
    
}