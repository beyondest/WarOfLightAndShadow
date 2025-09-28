using SparFlame.Components.MainGameplay;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.MainGameplay.City
{
    public class CityModeRootAuthoring : MonoBehaviour
    {
        public bool isLight;
        private class CityModeRootAuthoringBaker : Baker<CityModeRootAuthoring>
        {
            public override void Bake(CityModeRootAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Renderable);
                if(authoring.isLight)
                    AddComponent<CityLightModelRoot>(entity);
                else AddComponent<CityDarkModelRoot>(entity);
            }
        }
    }
}