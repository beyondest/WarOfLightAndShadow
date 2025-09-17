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
                    var buffer = AddBuffer<LoadingGridInfo>(ecoEntity);
                    AddComponent(ecoEntity, new MapInfo
                    {
                        CameraMaxCoordinate = item.camMaxCoordinate,
                        CameraMinCoordinate = item.camMinCoordinate,
                    });
                    foreach (var info in item.loadingGridInfos)
                    {
                        buffer.Add(info);
                    }
                    ecoBuffer.Add(new EcoEntityData
                    {
                        EcoType = item.ecoType,
                        EcoEntity = ecoEntity,
                    });
                }
            }
        }
    }
}