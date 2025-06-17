using System;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Interact
{
    public class GarrisonBuffSystemAuthoring : MonoBehaviour
    {
        public GarrisonBuffConfig config;
        private class GarrisonBuffSystemAuthoringBaker : Baker<GarrisonBuffSystemAuthoring>
        {
            public override void Bake(GarrisonBuffSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity,authoring.config);
            }
        }
    }

    [Serializable]
    public struct GarrisonBuffConfig : IComponentData
    { 
        public float buildingDamageReduceScale;
        public float unitDamageReduceScale;
        public int minCountToTriggerGarrisonBuff;
    }

    public struct BuildingGarrisonBuff : IComponentData, IEnableableComponent
    {
    }

    public struct UnitGarrisonBuff : IComponentData, IEnableableComponent
    {
        
    }
}