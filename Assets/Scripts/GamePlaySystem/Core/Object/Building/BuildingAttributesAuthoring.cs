using SparFlame.GamePlaySystem.General;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Serialization;

namespace SparFlame.GamePlaySystem.Building
{
    public class BuildingAttributesAuthoring : MonoBehaviour
    {
        public BuildingType buildingType;
        public BuildingState buildingInitialState = BuildingState.Idle;
        public int subTypeIndex;
        public ConjuringShrineType conjuringShrineTypeCheckList;
        public FortificationType fortificationTypeCheckList;
        public DwellingType dwellingTypeCheckList;
        public GeneratorType generatorTypeCheckList;
        public OrnamentType ornamentTypeCheckList;
        
        
        private class BuildingAttributesAuthoringBaker : Baker<BuildingAttributesAuthoring>
        {
            public override void Bake(BuildingAttributesAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.WorldSpace);
                AddComponent(entity, new BuildingAttr
                {
                    Type = authoring.buildingType,
                    State = authoring.buildingInitialState,
                    SubTypeIndex = authoring.subTypeIndex
                });
            }
        }
    }

    public enum BuildingState
    {
        Idle = 0,
        Constructing =1,
        Constructed = 2,
        Working = 3,
        Worked = 4,
        UnderAttack = 5
    }

    public struct BuildingAttr : IComponentData
    {
        public BuildingType Type;
        public BuildingState State;
        public int SubTypeIndex;
    }


}