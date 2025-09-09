using System;
using Sirenix.OdinInspector;
using SparFlame.Components.MainGameplay;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SparFlame.Systems.MainGameplay.ArmyGroup
{
    public class ArmyGroupUnitManageSystemAuthoring : MonoBehaviour
    {
        [AssetsOnly] public GameObject lightArmyGroupPrefab;
        [AssetsOnly] public GameObject darkArmyGroupPrefab;
        public float3 hidePosition;
        private class ArmyGroupManageSystemAuthoringBaker : Baker<ArmyGroupUnitManageSystemAuthoring>
        {
            public override void Bake(ArmyGroupUnitManageSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new ArmyGroupConfig
                {
                    DarkArmyGroupPrefab = GetEntity(authoring.darkArmyGroupPrefab, TransformUsageFlags.Dynamic),
                    LightArmyGroupPrefab = GetEntity(authoring.lightArmyGroupPrefab, TransformUsageFlags.Dynamic),
                    HidePosition = authoring.hidePosition,
                });
            }
        }
    }

    
}