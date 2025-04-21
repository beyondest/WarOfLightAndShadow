using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Resource
{
    public class DwellingAttributeAuthoring : MonoBehaviour
    {
        private class DwellingAttributeAuthoringBaker : Baker<DwellingAttributeAuthoring>
        {
            public override void Bake(DwellingAttributeAuthoring authoring)
            {
            }
        }
    }

    public struct DwellingAttr : IComponentData
    {
        public ResourceType ResourceType;
        public int Amount;
    }
}