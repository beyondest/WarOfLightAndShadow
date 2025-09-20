using Sirenix.OdinInspector;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.UI.MainGameplay.CityWindow
{
    public class ArmyGroupGarrisonWindowTransferAuthoring : MonoBehaviour
    {

        [AssetsOnly] public GameObject cityWindow3DPrefab;
        public Vector3 offset;
        private class
            ArmyGroupGarrisonWindowTransferAuthoringBaker : Baker<
            ArmyGroupGarrisonWindowTransferAuthoring>
        {
            public override void Bake(ArmyGroupGarrisonWindowTransferAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponentObject(entity, new CityWindowConfig
                {
                    Prefab = authoring.cityWindow3DPrefab,
                    Offset = authoring.offset
                });
            }
        }
        
    }

    public class CityWindowConfig : IComponentData
    {
        public GameObject Prefab;
        public Vector3 Offset;
    }
}