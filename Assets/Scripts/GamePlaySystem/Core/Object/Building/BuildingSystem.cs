using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Building.GamePlaySystem.Core.Object.Building
{
    public partial class BuildingSystem : SystemBase
    {
        private NativeHashMap<FixedString64Bytes, Entity> _buildingName2Entities;

        protected override void OnCreate()
        {
            _buildingName2Entities = new NativeHashMap<FixedString64Bytes, Entity>(100,Allocator.Persistent);
            RequireForUpdate<BuildingSlot>();
        }

        protected override void OnUpdate()
        {
            var buffer = SystemAPI.GetSingletonBuffer<BuildingSlot>();
            Debug.Log($"{buffer[0].Name}");
        }

        protected override void OnDestroy()
        {
            _buildingName2Entities.Dispose();
        }
    }
}