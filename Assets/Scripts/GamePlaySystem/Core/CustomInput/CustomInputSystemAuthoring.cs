using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Authoring;

namespace SparFlame.GamePlaySystem.Mouse
{
    public class CustomInputSystemAuthoring : MonoBehaviour
    {
        public PhysicsCategoryTags clickableLayer;
        public PhysicsCategoryTags mouseRayLayer;
        public float raycastDistance = 1000f;
        public float doubleClickThreshold = 0.15f;

        public int leftClickIndex = 0;
        [Tooltip("Pressed")]
        public KeyCode focusKey = KeyCode.F;
        [Tooltip("Toggle")]
        public KeyCode changeFactionKey = KeyCode.C;
        [Tooltip("Pressed")]
        public KeyCode addUnitKey = KeyCode.LeftShift;

        class Baker : Baker<CustomInputSystemAuthoring>
        {
            public override void Bake(CustomInputSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new CustomInputSystemConfig
                {
                    ClickableLayerMask = authoring.clickableLayer.Value,
                    MouseRayLayerMask = authoring.mouseRayLayer.Value,
                    RaycastDistance = authoring.raycastDistance,
                    DoubleClickThreshold = authoring.doubleClickThreshold,
                    LeftClickIndex = authoring.leftClickIndex,
                    FocusKey = authoring.focusKey,
                    ChangeFactionKey = authoring.changeFactionKey,
                    AddUnitKey = authoring.addUnitKey,
                });
                AddComponent(entity, new CustomInputSystemData
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
    public struct CustomInputSystemConfig : IComponentData
    {
        public uint ClickableLayerMask;
        public uint MouseRayLayerMask;
        public float RaycastDistance;
        public float DoubleClickThreshold;
        public int LeftClickIndex;
        public KeyCode FocusKey;
        public KeyCode ChangeFactionKey;
        public KeyCode AddUnitKey;
    }


    /// <summary>
    /// The Only Reason to use the mouse system is to reduce the times of using raycast
    /// </summary>
    public struct CustomInputSystemData : IComponentData
    {
        public ClickFlag ClickFlag;
        public ClickType ClickType;
        /// <summary>
        /// if no raycast hit , hitEntity is Entity.Null
        /// </summary>
        public Entity HitEntity;
        public float3 HitPosition;
        public float3 MousePosition;
        public bool Focus;
        public bool ChangeFaction;
        public bool AddUnit;
    }
}