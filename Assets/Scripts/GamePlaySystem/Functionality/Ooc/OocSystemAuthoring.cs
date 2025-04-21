using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Ooc
{
    public class NewBakerScript : MonoBehaviour
    {
        public float buildingOocSeconds = 10f;
        public float unitOocSeconds = 10f;
        class NewBakerScriptBaker : Baker<NewBakerScript>
        {
            public override void Bake(NewBakerScript authoring)
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
