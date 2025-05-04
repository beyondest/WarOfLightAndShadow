using System;
using System.Collections.Generic;
using SparFlame.GamePlaySystem.General;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Resource
{
    public class OccupiedManageSystemAuthoring : MonoBehaviour
    {

        public GameObject allyOccupiedRef;
        public GameObject enemyOccupiedRef;
        public GameObject neutralOccupiedRef;
        public float crystalChangeRadius;
        private class OccupiedManageSystemAuthoringBaker : Baker<OccupiedManageSystemAuthoring>
        {
            public override void Bake(OccupiedManageSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new OccupiedManageSystemConfig
                {
                    CrystalChangeRadiusSq = authoring.crystalChangeRadius * authoring.crystalChangeRadius,
                    AllyOccupiedRef = GetEntity(authoring.allyOccupiedRef, TransformUsageFlags.None),
                    EnemyOccupiedRef = GetEntity(authoring.enemyOccupiedRef, TransformUsageFlags.None),
                    NeutralOccupiedRef = GetEntity(authoring.neutralOccupiedRef, TransformUsageFlags.None),
                });
            }
        }
    }


    
    
    public struct OccupiedManageSystemConfig : IComponentData
    {
        public float CrystalChangeRadiusSq;
        public Entity AllyOccupiedRef;
        public Entity EnemyOccupiedRef;
        public Entity NeutralOccupiedRef;
    }
}