using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SparFlame.GamePlaySystem.CustomInput
{
    public class InputCameraFlyModeAuthoring : MonoBehaviour
    {
        private class InputCameraFlyModeAuthoringBaker : Baker<InputCameraFlyModeAuthoring>
        {
            public override void Bake(InputCameraFlyModeAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent<InputCameraFlyData>(entity);
            }
        }
    }

    public struct InputCameraFlyData : IComponentData
    {
        public bool Enabled;
        public float2 LookDelta;
        public float2 Move;
        public bool SpeedUp;
        public bool FlyUp;
        public bool FlyDown;
        public float2 Zoom;

    }
}