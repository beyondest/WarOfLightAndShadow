using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Spawn
{
    public class BuildingConjureSystemAuthoring : MonoBehaviour
    {
        private class ConjureSystemAuthoringBaker : Baker<BuildingConjureSystemAuthoring>
        {
            public override void Bake(BuildingConjureSystemAuthoring authoring)
            {
            }
        }
    }

    public struct ConjureSystemConfig : IComponentData
    {
        
    }
}