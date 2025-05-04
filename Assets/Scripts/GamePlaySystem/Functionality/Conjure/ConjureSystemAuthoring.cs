using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Spawn
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
    public struct ConjureRequest : IComponentData
    {
        public Entity UnitPrefab;
        public Entity BuildingEntity;
        public int Count;
    }

    public struct ConjuringTag : IComponentData
    {
        
    }


}