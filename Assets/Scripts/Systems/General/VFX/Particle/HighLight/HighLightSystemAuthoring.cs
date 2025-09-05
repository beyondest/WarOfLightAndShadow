using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SparFlame.Systems.General.VFX
{
    public class HighLightSystemAuthoring : MonoBehaviour
    {
        public HighLightSystemConfigInspector config;

        private class HighLightSystemAuthoringBaker : Baker<HighLightSystemAuthoring>
        {
            public override void Bake(HighLightSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new HighLightSystemConfig
                {
                    BuildingHighLightScale = authoring.config.buildingHighLightScale,
                    ResourceHighLightScale = authoring.config.resourceHighLightScale,
                    UnitHighLightScale = authoring.config.unitHighLightScale,
                    AllyHighLightColor = new float4(authoring.config.playerHighLightColor.r,
                        authoring.config.playerHighLightColor.g, authoring.config.playerHighLightColor.b,
                        authoring.config.playerHighLightColor.a),
                    HostileHighLightColor = new float4(authoring.config.enemyHighLightColor.r, authoring.config.enemyHighLightColor.g,
                        authoring.config.enemyHighLightColor.b, authoring.config.enemyHighLightColor.a),
                    NeutralHighLightColor = new float4(authoring.config.neutralHighLightColor.r,authoring.config.neutralHighLightColor.g,
                        authoring.config.neutralHighLightColor.b, authoring.config.neutralHighLightColor.a),
                });
            }
        }
    }

    [Serializable]
    public struct HighLightSystemConfigInspector : IComponentData
    {
        public float unitHighLightScale;
        public float buildingHighLightScale;
        public float resourceHighLightScale;
        public Color playerHighLightColor;
        public Color enemyHighLightColor;
        public Color neutralHighLightColor;
    }

    public struct HighLightSystemConfig : IComponentData
    {
        public float UnitHighLightScale;
        public float BuildingHighLightScale;
        public float ResourceHighLightScale;
        public float4 AllyHighLightColor;
        public float4 HostileHighLightColor;
        public float4 NeutralHighLightColor;
    }
}