using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using Unity.Entities;

namespace SparFlame.GamePlaySystem.Building
{
    public class BuildingSystemAuthoring : MonoBehaviour
    {
        public float buildingGarrisonRadius = 1f;
        private class BuildingSystemAuthoringBaker : Unity.Entities.Baker<BuildingSystemAuthoring>
        {
            public override void Bake(BuildingSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new BuildingConfig
                {
                    BuildingGarrisonRadiusSq = authoring.buildingGarrisonRadius * authoring.buildingGarrisonRadius,
                });
            }
        }
    }


    
    public struct BuildingConfig : IComponentData
    {
        public float BuildingGarrisonRadiusSq;
    }

    public enum BuildingType 
    {
        Fortifications = 0,
        Workshops = 1,
        ConjuringShrines = 2,
        Dwellings = 3,
        Ornaments = 4,
    }

    public enum FortificationType
    {
        Wall = 0,
        AttackableTower = 1,
        HealableTower = 2,
        BuffTower = 3,
        DebuffTower = 4
    }

    public enum WorkshopType
    {
        EssenceConvertor = 0,
        EssenceProducer = 1
    }

    public enum ConjuringShrineType
    {
        Melee = 0,
        Ranged = 1,
        Magic = 2,
        Harvest = 3
    }

    public enum DwellingType
    {
        Lightness = 0,
        Darkness = 1,
    }

    public enum OrnamentType
    {
        Type0 = 0
    }
    
    public enum AreaType
    {
        Walkable = 0,
        NotWalkable = 1,
        Jump = 2,
        
        // Not attackable cost
        Cost00 = 3,
        Cost01 = 4,
        Cost02 = 5,
        Cost03 = 6,
        Cost04 = 7,
        
        // Attackable cost
        Cost10 = 13,
        Cost11 = 14,
        Cost12 = 15,
        Cost13 = 16,
        Cost14 = 17,
        Cost15 = 18,
    }



}

