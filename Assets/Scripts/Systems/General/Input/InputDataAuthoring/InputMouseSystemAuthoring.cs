using SparFlame.Components.Input;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Authoring;

namespace SparFlame.Systems.General.Input
{
    public class InputMouseSystemAuthoring : MonoBehaviour
    {
        public PhysicsCategoryTags clickableLayer;
        public PhysicsCategoryTags mouseRayLayer;
        public float raycastDistance = 1000f;
        public float doubleClickThreshold = 0.15f;

        [Header("Player Config")]

        public int leftClickIndex;
        public int rightClickIndex = 1;

        class Baker : Baker<InputMouseSystemAuthoring>
        {
            public override void Bake(InputMouseSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new CustomMouseSystemConfig
                {
                    ClickableLayerMask = authoring.clickableLayer.Value,
                    MouseRayLayerMask = authoring.mouseRayLayer.Value,
                    RaycastDistance = authoring.raycastDistance,
                    DoubleClickThreshold = authoring.doubleClickThreshold,
            
                });
                AddComponent(entity, new InputMouseData
                {
                    ClickFlag = ClickFlag.None,
                    ClickType = ClickType.None,
                    HitEntity = Entity.Null,
                    HitPosition = float3.zero,
                    MousePosition = float3.zero
                });
                AddComponent(entity, new CustomMouseKeyMapping
                {
                    LeftClickIndex = authoring.leftClickIndex,
                    RightClickIndex = authoring.rightClickIndex,
                });
            }
        } 
    }
    public struct CustomMouseKeyMapping : IComponentData
    {
        // General
        public int LeftClickIndex;
        public int RightClickIndex;
    }
    
    public struct CustomMouseSystemConfig : IComponentData
    {
        public uint ClickableLayerMask;
        public uint MouseRayLayerMask;
        public float RaycastDistance;
        public float DoubleClickThreshold;
    }




    

    
}