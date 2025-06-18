using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace SparFlame.Systems.Hints
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


}