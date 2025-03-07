using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace SparFlame.GamePlaySystem.Mouse
{
    public class MouseSystemAuthoring : MonoBehaviour
    {
        public LayerMask clickableLayer;
        public LayerMask mouseRayLayer;
        public float raycastDistance = 1000f;
        public float doubleClickThreshold = 0.15f;

        public int leftClickIndex = 0;

        class Baker : Baker<MouseSystemAuthoring>
        {
            public override void Bake(MouseSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new MouseSystemConfig
                {
                    ClickableLayerMask = (uint)authoring.clickableLayer.value,
                    MouseRayLayerMask = (uint)authoring.mouseRayLayer.value,
                    RaycastDistance = authoring.raycastDistance,
                    DoubleClickThreshold = authoring.doubleClickThreshold,
                    LeftClickIndex = authoring.leftClickIndex
                });
                AddComponent(entity, new MouseSystemData
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
    public struct MouseSystemConfig : IComponentData
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
    public struct MouseSystemData : IComponentData
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