using SparFlame.Components.General;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Database
{
    public class EcoDatabaseAuthoring : MonoBehaviour
    {
        private class EcoDatabaseAuthoringBaker : Baker<EcoDatabaseAuthoring>
        {
            public override void Bake(EcoDatabaseAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                var ecoBuffer = AddBuffer<EcoEntityData>(entity);
                var items = DatabaseManager.EcoDatabaseSo.items;
                foreach (var item in items)
                {
                    var ecoEntity = CreateAdditionalEntity(TransformUsageFlags.None);
                    AddComponent(ecoEntity, new MapInfo
                    {
                        CameraMaxCoordinate = item.camMaxCoordinate,
                        CameraMinCoordinate = item.camMinCoordinate,
                    });
                    
                    ecoBuffer.Add(new EcoEntityData
                    {
                        EcoType = item.ecoType,
                        EcoEntity = ecoEntity,
                    });
                    AddComponent(ecoEntity,item.loadingPositionInfo);
                }
            }
        }
    }
}