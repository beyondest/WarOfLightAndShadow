using System;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.Serialization;

namespace SparFlame.GamePlaySystem.PopNumber
{
    public class PopNumberSystemAuthoring : MonoBehaviour
    {
        public GameObject popNumberPrefab;
        
        [Header("PopNumber Animation Config")]
        [Tooltip("Control the fade vertical direction of the pop number")]
        public float verticalMovementOffset = 2f;
        [Tooltip("Fade out time")]
        public float movementTime = 2f;
        [Tooltip("Will apply scale = scale x (1 + scaleOffset * easing) \n" +
                 " Notice : scale offset better be 0, or it will make wide space more little or larger ")]
        public float scaleOffset = 1f;
        [Tooltip("Will apply z += zMovementOffset * easing")]
        public float zMovementOffset;
        
        
        
        [Header("PopNumber Show Config")]
        [Tooltip("The only reason to adjust this, is when camera has offset in y or z axis and that will cause number not horizontal")]
        public float glyphZOffset = 0.001f;
        [Tooltip("Will affect the pop number wide space within each number")]
        public float glyphWidth = 0.07f;
        
        // public Color[] popNumberColors;
        public PopNumberTypeToColor[] popNumberTypeToColors;
        
        private class Baker : Baker<PopNumberSystemAuthoring>
        {
            public override void Bake(PopNumberSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new PopNumberConfig
                {
                    GlyphPrefab = GetEntity(authoring.popNumberPrefab, TransformUsageFlags.None),
                    ScaleOffset = authoring.scaleOffset,
                    VerticalMovementOffset = authoring.verticalMovementOffset,
                    ZMovementOffset = authoring.zMovementOffset,
                    MovementTime = authoring.movementTime,
                    GlyphZOffset = authoring.glyphZOffset,
                    GlyphWidth = authoring.glyphWidth
                });

                var buffer = AddBuffer<PopNumberColorConfig>(entity);
                for(var i = 0; i < 10; i++    )
                {
                    buffer.Add(new PopNumberColorConfig {  });
                }
                foreach (var pair in authoring.popNumberTypeToColors)
                {
                    var color = pair.popNumberColor;
                    buffer.ElementAt((int)pair.popNumberType) = new PopNumberColorConfig
                    {
                        Color = new float4(color.r, color.g, color.b, color.a),
                    };
                }
            }
        }
    }

    [Serializable]
    public struct PopNumberTypeToColor
    {
        public PopNumberType popNumberType;
        public Color popNumberColor;
    }
    
    public enum PopNumberType
    {
        DamageTaken = 0,
        DamageDealt = 1,
        AllyHealed = 2,
        EnemyHealed = 3,
        AllyHarvest= 4,
        EnemyHarvest = 5
    }
    
    public struct PopNumberConfig : IComponentData
    {
        public Entity GlyphPrefab;
        public float VerticalMovementOffset;
        public float MovementTime;
        public float ZMovementOffset;
        public float ScaleOffset;
        public float GlyphZOffset;
        public float GlyphWidth;
    }

    public struct PopNumberColorConfig : IBufferElementData
    {
        public float4 Color;
    }
    
    public struct PopNumberRequest : IComponentData
    {
        public int Value;
        public int ColorId;
        public float3 Position;
        public float Scale;
    }

    public struct PopNumberData : IComponentData
    {
        public float SpawnTime;
        public float OriginalY;
    }
}