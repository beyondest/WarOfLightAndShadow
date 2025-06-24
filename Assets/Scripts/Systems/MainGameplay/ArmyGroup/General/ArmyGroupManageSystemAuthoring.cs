using System;
using Sirenix.OdinInspector;
using SparFlame.Components.MainGameplay;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SparFlame.Systems.MainGameplay.ArmyGroup
{
    public class ArmyGroupManageSystemAuthoring : MonoBehaviour
    {
        [AssetsOnly] public GameObject lightArmyGroupPrefab;
        [AssetsOnly] public GameObject darkArmyGroupPrefab;
        public float3 hidePosition;
        private class ArmyGroupManageSystemAuthoringBaker : Baker<ArmyGroupManageSystemAuthoring>
        {
            public override void Bake(ArmyGroupManageSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new ArmyGroupManageConfig
                {
                    DarkArmyGroupPrefab = GetEntity(authoring.darkArmyGroupPrefab, TransformUsageFlags.Dynamic),
                    LightArmyGroupPrefab = GetEntity(authoring.lightArmyGroupPrefab, TransformUsageFlags.Dynamic),
                    HidePosition = authoring.hidePosition,
                });
            }
        }
    }

    
}