using SparFlame.Components.MainGameplay;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Database
{
    public class ArmyGroupDatabaseAuthoring : MonoBehaviour
    {
        private class ArmyGroupDatabaseAuthoringBaker : Baker<ArmyGroupDatabaseAuthoring>
        {
            public override void Bake(ArmyGroupDatabaseAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                var buffer = AddBuffer<ArmyGroupEntityPrefabData>(entity);
                foreach (var dataItem in DatabaseManager.ArmyGroupDatabaseSo.items)
                {
                    buffer.Add(new ArmyGroupEntityPrefabData
                    {
                        Prefab = GetEntity(dataItem.prefab, TransformUsageFlags.Dynamic),
                        PrefabId = dataItem.id
                    });
                }
            }
        }
    }
}