using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.CameraControl
{
    public class FlyCameraControlSystemAuthoring : MonoBehaviour
    {
        public float lookSpeedH = 2f;
        public float lookSpeedV = 2f;
        public float zoomSpeed = 2f;
        public float flySpeed = 2f;
        public float speedUpMultiplier = 2f;
        
        private class FlyCameraControlSystemBaker : Baker<FlyCameraControlSystemAuthoring>
        {
            public override void Bake(FlyCameraControlSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                
                AddComponent(entity, new FlyCameraControlConfig
                {
                    LookSpeedV = authoring.lookSpeedV,
                    LookSpeedH = authoring.lookSpeedH,
                    ZoomSpeed = authoring.zoomSpeed,
                    FlySpeed = authoring.flySpeed,
                    SpeedUpMultiplier = authoring.speedUpMultiplier,
                });
            }
        }
    }

    public struct FlyCameraControlConfig : IComponentData
    {
        public float LookSpeedH;
        public float LookSpeedV;
        public float ZoomSpeed;
        public float FlySpeed;
        public float SpeedUpMultiplier;
    }
}

