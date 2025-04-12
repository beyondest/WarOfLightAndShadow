using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Generate
{
    public class BuildingGenerateSystemAuthoring : MonoBehaviour
    {
        private class BuildingGenerateSystemAuthoringBaker : Baker<BuildingGenerateSystemAuthoring>
        {
            public override void Bake(BuildingGenerateSystemAuthoring authoring)
            {
            }
        }
    }

    public struct BuildingGenerateSystemConfig : IComponentData
    {
        
    }
    
}