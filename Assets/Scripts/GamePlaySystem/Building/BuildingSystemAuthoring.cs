using UnityEngine;
using Unity.Entities;
namespace SparFlame.GamePlaySystem.Building
{
    public class BuildingSystemAuthoring : MonoBehaviour
    {
        public float buildingGarrisonRadius = 1f;
        private class BuildingSystemAuthoringBaker : Unity.Entities.Baker<BuildingSystemAuthoring>
        {
            public override void Bake(BuildingSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new BuildingConfig
                {
                    BuildingGarrisonRadiusSq = authoring.buildingGarrisonRadius * authoring.buildingGarrisonRadius,
                });
            }
        }
    }

    public struct BuildingConfig : IComponentData
    {
        public float BuildingGarrisonRadiusSq;
    }



}