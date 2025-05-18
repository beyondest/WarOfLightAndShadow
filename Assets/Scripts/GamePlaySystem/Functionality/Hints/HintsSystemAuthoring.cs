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


    public enum HintType
    {
        None = 0,
        ResourceTierNotMatch = 1,
        ResourceAmountNotEnough = 2,
        ConstructOverlapping = 3,
        ConstructOnNotConstructable = 4,
        CrystalUnderAttack = 5
    }

    public struct HintRequest : IComponentData
    {
        public float3 Position;
        public HintType Type;
    }
    
    
    
}