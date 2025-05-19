using System;
using System.Collections.Generic;
using SparFlame.GamePlaySystem.General;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Resource
{
    public class OccupiedManageSystemAuthoring : MonoBehaviour
    {

        [Tooltip("This is also constructable radius around crystals")]
        public float crystalAffectRadius;

        public float constructGridSize = 2;
        private class OccupiedManageSystemAuthoringBaker : Baker<OccupiedManageSystemAuthoring>
        {
            public override void Bake(OccupiedManageSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new CrystalAffectMapRadiusSq
                {
                    Value = authoring.crystalAffectRadius * authoring.crystalAffectRadius,
                });
                AddComponent(entity, new ConstructGridSize
                {
                    Value = authoring.constructGridSize,
                });
            }
        }
    }


    public struct ConstructGridSize : IComponentData
    {
        public float Value;
    }
    
    public struct CrystalAffectMapRadiusSq : IComponentData
    {
        public float Value;
    }
}