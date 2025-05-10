using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Ooc
{
    public class OocSystemAuthoring : MonoBehaviour
    {
        public float buildingOocSeconds = 10f;
        public float unitOocSeconds = 10f;
        class Baker : Baker<OocSystemAuthoring>
        {
            public override void Bake(OocSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new OocSystemConfig
                {
                    BuildingOocSeconds = authoring.buildingOocSeconds,
                    UnitOocSeconds = authoring.unitOocSeconds,
                });
            }
        }
    }

    public struct OocTag : IComponentData
    {
        public float Seconds;
    }

    public struct OocSystemConfig : IComponentData
    {
        public float BuildingOocSeconds;
        public float UnitOocSeconds;
    }
}
