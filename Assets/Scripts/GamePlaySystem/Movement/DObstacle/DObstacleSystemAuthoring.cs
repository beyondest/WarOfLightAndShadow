using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Movement
{
    public class DynamicObstacleSystemAuthoring : MonoBehaviour
    {
        
        
        public Dictionary<DObstacleType, GameObject> TypePrefabMap ;
        private class Baker : Baker<DynamicObstacleSystemAuthoring>
        {
            public override void Bake(DynamicObstacleSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponentObject(entity, new DObstacleTypePrefabMap
                {
                    TypePrefabMap = authoring.TypePrefabMap,
                });
            }
        }
    }

    public enum DObstacleType
    {
        Unit,
        Building1X,
        Building2X,
        Building4X,
    }

    public class DObstacleTypePrefabMap : IComponentData
    {
        public Dictionary<DObstacleType, GameObject> TypePrefabMap;
    }
    
}