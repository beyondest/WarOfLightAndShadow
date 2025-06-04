using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Map
{
    public class TileDataAuthoring : MonoBehaviour
    {
        private class TileTypeAuthoringBaker : Baker<TileDataAuthoring>
        {
            public override void Bake(TileDataAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new TileData());
                AddComponent(entity, new CrystalRecordData
                {
                    RecordCount = 0
                });
                AddBuffer<EnvEntities>(entity);
            }
        }
    }

    public struct CrystalRecordData : IComponentData
    {
        public int RecordCount;
    }

    public struct EnvEntities : IBufferElementData
    {
        public Entity Value;
    }

}