using System;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Fow
{
    public class FogOfWarTagAuthoring : MonoBehaviour
    {
        private class FogOfWarTagAuthoringBaker : Baker<FogOfWarTagAuthoring>
        {
            public override void Bake(FogOfWarTagAuthoring authoring)
            {
 
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<FowTag>(entity);
            }
        }
    }

    public struct FowTag : IComponentData
    {
        
    }
    
}