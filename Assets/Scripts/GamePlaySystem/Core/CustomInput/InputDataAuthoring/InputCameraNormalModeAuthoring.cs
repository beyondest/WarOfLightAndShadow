using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SparFlame.GamePlaySystem.CustomInput
{
    public class InputCameraNormalModeAuthoring : MonoBehaviour
    {
        private class InputCameraNormalModeAuthoringBaker : Baker<InputCameraNormalModeAuthoring>
        {
            public override void Bake(InputCameraNormalModeAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new InputCameraNormalData());
            }
        }
    }
    public struct InputCameraNormalData : IComponentData
    {
        public bool Enabled;
        public float2 Movement;
        public float2 ZoomCamera;
        public float RotateCamera;
        public bool EdgeScrolling;
        public bool DraggingCamera;
        public bool DragCameraStart;
        public bool SpeedUp;
    }
}