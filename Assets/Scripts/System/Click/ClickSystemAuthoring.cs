using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace SparFlame.System.Click
{
    public class ClickSystemAuthoring : MonoBehaviour
    {
        public LayerMask clickableLayer;
        public LayerMask mouseRayLayer;
        public float raycastDistance;
        public float doubleClickThreshold;

        public int leftClickIndex;

        class Baker : Baker<ClickSystemAuthoring>
        {
            public override void Bake(ClickSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new ClickSystemConfig
                {
                    ClickableLayerMask = (uint)authoring.clickableLayer.value,
                    MouseRayLayerMask = (uint)authoring.mouseRayLayer.value,
                    RaycastDistance = authoring.raycastDistance,
                    DoubleClickThreshold = authoring.doubleClickThreshold,
                    LeftClickIndex = authoring.leftClickIndex
                });
                AddComponent(entity, new ClickSystemData
                {
                    ClickFlag = ClickFlag.None,
                    ClickType = ClickType.None,
                    HitEntity = Entity.Null,
                    HitPosition = float3.zero,
                    MousePosition = float3.zero
                });
            }
        } 
    }

    public enum ClickFlag
    {
        Start,
        Clicking,
        End,
        DoubleClick,
        None
    }
    public enum ClickType
    {
        Left,
        Right,
        Middle,
        None
    }
    public struct ClickSystemConfig : IComponentData
    {
        public uint ClickableLayerMask;
        public uint MouseRayLayerMask;
        public float RaycastDistance;
        public float DoubleClickThreshold;
        public int LeftClickIndex;
    }


    /// <summary>
    /// The Only Reason to use the click system is to reduce the times of using raycast
    /// </summary>
    public struct ClickSystemData : IComponentData
    {
        public ClickFlag ClickFlag;
        public ClickType ClickType;
        /// <summary>
        /// if no raycast hit , hitEntity is Entity.Null
        /// </summary>
        public Entity HitEntity;
        public float3 HitPosition;
        public float3 MousePosition;
    }
}