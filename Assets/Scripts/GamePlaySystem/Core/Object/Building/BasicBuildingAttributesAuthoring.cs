using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Building
{
    public class BasicBuildingAttributesAuthoring : MonoBehaviour
    {
        public int id;
        public BuildingDatabaseSo2 data2;
        private class BasicBuildingAttributesAuthoringBaker : Baker<BasicBuildingAttributesAuthoring>
        {
            public override void Bake(BasicBuildingAttributesAuthoring authoring)
            {
                var entry = authoring.data2.buildingsData.Find(d => d.id == authoring.id);
                if (entry == null)
                {
                    Debug.LogError($"Cannot find building data for id: {authoring.id}");
                    return;
                }
                var entity = GetEntity(TransformUsageFlags.WorldSpace);
                AddComponent(entity, new BuildingAttr
                {
                    Type = entry.buildingType,
                    State = entry.buildingInitialState,
                    SubTypeIndex = entry.subKey
                });
            }
        }
    }
}