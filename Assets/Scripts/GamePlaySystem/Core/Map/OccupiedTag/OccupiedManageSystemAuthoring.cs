using System;
using System.Collections.Generic;
using SparFlame.GamePlaySystem.General;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Resource
{
    public class OccupiedManageSystemAuthoring : MonoBehaviour
    {

        public float crystalAffectRadius;
        private class OccupiedManageSystemAuthoringBaker : Baker<OccupiedManageSystemAuthoring>
        {
            public override void Bake(OccupiedManageSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new CrystalAffectRadiusSq
                {
                    Value = authoring.crystalAffectRadius * authoring.crystalAffectRadius,
                });
            }
        }
    }


    
    
    public struct CrystalAffectRadiusSq : IComponentData
    {
        public float Value;
    }
}