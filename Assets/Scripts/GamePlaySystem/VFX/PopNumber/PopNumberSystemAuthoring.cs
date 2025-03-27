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
        public Color[] popNumberColors;
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
                foreach (var managedColor in authoring.popNumberColors)
                {
                    var color = new float4(managedColor.r, managedColor.g, managedColor.b, managedColor.a);
                    buffer.Add(new PopNumberColorConfig { Color = color });
                }
            }
        }
    }

    public enum PopNumberType
    {
        DamageTaken,
        DamageDealt,
        AllyHealed,
        EnemyHealed,
        Resource
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
        public PopNumberType Type;
    }

    public struct PopNumberData : IComponentData
    {
        public float SpawnTime;
        public float OriginalY;
        public PopNumberType Type;
    }
}