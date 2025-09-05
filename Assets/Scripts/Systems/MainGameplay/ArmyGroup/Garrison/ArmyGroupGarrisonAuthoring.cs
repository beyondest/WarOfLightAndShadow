using System;
using SparFlame.Components.MainGameplay;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SparFlame.Systems.MainGameplay.ArmyGroup
{
    public class ArmyGroupGarrisonAuthoring : MonoBehaviour
    {
        public ArmyGroupGarrisonSystemConfig config;
        private class ArmyGroupGarrisonAuthoringBaker : Baker<ArmyGroupGarrisonAuthoring>
        {
            public override void Bake(ArmyGroupGarrisonAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity,authoring.config);
            }
        }
    }



   
}