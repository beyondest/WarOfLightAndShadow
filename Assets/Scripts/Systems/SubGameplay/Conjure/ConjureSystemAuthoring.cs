using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.SubGameplay.Conjure
{
    public class ConjureSystemAuthoring : MonoBehaviour
    {
        private class ConjureSystemAuthoringBaker : Baker<ConjureSystemAuthoring>
        {
            public override void Bake(ConjureSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new ConjureSystemConfig
                {
                    
                });
            }
        }
    }


    public struct ConjureSystemConfig : IComponentData
    {
        
    }


}