using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SparFlame.GamePlaySystem.CameraControl
{
    public class CameraControlPlusSystemAuthoring : MonoBehaviour
    {
        [Header("General")] public bool edgeMoveEnabled;

        [Header("Horizontal Translation")] [SerializeField]
        private float maxSpeed = 5f;

        [Header("Horizontal Translation")] [SerializeField]
        private float acceleration = 10f;

        [Header("Horizontal Translation")] [SerializeField]
        private float damping = 15f;

        [Header("Vertical Translation")] [SerializeField]
        private float stepSize = 2f;

        [Header("Vertical Translation")] [SerializeField]
        private float zoomDampening = 7.5f;

        [Header("Vertical Translation")] [SerializeField]
        private float minHeight = 5f;

        [Header("Vertical Translation")] [SerializeField]
        private float maxHeight = 50f;

        [Header("Vertical Translation")] [SerializeField]
        private float zoomSpeed = 2f;

        [Header("Rotation")] [SerializeField] private float maxRotationSpeed = 1f;
        public float rotationSmoothness = 1.0f;
        
        [Header("Edge Movement")] [SerializeField] [Range(0f, 0.1f)]
        private float edgeTolerance = 0.05f;

        [SerializeField] private float edgeMovementBaseSpeed = 1.0f;
        [SerializeField] private float speedUpFactor = 2.0f;
        
        private class CameraControlPlusSystemAuthoringBaker : Baker<CameraControlPlusSystemAuthoring>
        {
            public override void Bake(CameraControlPlusSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new CameraControlConfig
                {
                    Acceleration = authoring.acceleration,
                    EdgeMoveEnabled = authoring.edgeMoveEnabled,
                    Damping = authoring.damping,
                    EdgeTolerance = authoring.edgeTolerance,
                    MaxHeight = authoring.maxHeight,
                    MinHeight = authoring.minHeight,
                    StepSize = authoring.stepSize,
                    ZoomSpeed = authoring.zoomSpeed,
                    MaxRotationSpeed = authoring.maxRotationSpeed,
                    MaxSpeed = authoring.maxSpeed,
                    ZoomDamping = authoring.zoomDampening,
                    EdgeMovementBaseSpeed = authoring.edgeMovementBaseSpeed,
                    SpeedUpFactor = authoring.speedUpFactor,
                    RotationSmoothness = authoring.rotationSmoothness,
                });
            }
        }
    }


    public struct CameraFollowTag : IComponentData
    {
        
    }

    public struct CameraControlConfig : IComponentData
    {
        public bool EdgeMoveEnabled;
        public float MaxSpeed;
        public float Acceleration;
        public float Damping;
        public float StepSize;
        public float ZoomDamping;
        public float MinHeight;
        public float MaxHeight;
        public float ZoomSpeed;
        public float MaxRotationSpeed;
        public float EdgeTolerance;
        public float EdgeMovementBaseSpeed;
        public float SpeedUpFactor;
        public float RotationSmoothness;
    }


}