using SparFlame.GamePlaySystem.General;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Serialization;

namespace SparFlame.GamePlaySystem.Building
{
    public class BuildingAttributesAuthoring : MonoBehaviour
    {
        public BuildingType buildingType;
        public int subTypeIndex;
 
        
        private class BuildingAttributesAuthoringBaker : Baker<BuildingAttributesAuthoring>
        {
            public override void Bake(BuildingAttributesAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.WorldSpace);
                AddComponent(entity, new BuildingAttr
                {
                    Type = authoring.buildingType,
                    SubTypeIndex = authoring.subTypeIndex
                });
            }
        }
    }

    public enum BuildingState
    {
        Idle = 0,
        Constructing =1,
        Working = 2,
        UnderAttack = 3,
        // Worked = 4, // Obsolete
    }

    public struct ConstructingTag : IComponentData
    {
        
    }
    
    public struct BuildingAttr : IComponentData
    {
        public BuildingType Type;
        // public BuildingState CurBuildingState;
        public int SubTypeIndex;
        // public float BuildingStateCount;
    }


}