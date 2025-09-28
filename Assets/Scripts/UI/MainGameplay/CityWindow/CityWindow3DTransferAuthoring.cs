using Sirenix.OdinInspector;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.UI.MainGameplay.CityWindow
{
    public class CityWindow3DTransferAuthoring : MonoBehaviour
    {

        [AssetsOnly] public GameObject cityWindow3DPrefab;
        public Vector3 offset;
        private class
            ArmyGroupGarrisonWindowTransferAuthoringBaker : Baker<
            CityWindow3DTransferAuthoring>
        {
            public override void Bake(CityWindow3DTransferAuthoring authoring)
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