using SparFlame.GamePlaySystem.Units;
using Unity.Entities;
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
            }
        }
    }

    public struct ConjureAttr : IComponentData
    {
        public UnitType ConjuringType;
        public int TargetAmount;
        public int ConjuredAmount;
        public int RemainingTime;
    }
}