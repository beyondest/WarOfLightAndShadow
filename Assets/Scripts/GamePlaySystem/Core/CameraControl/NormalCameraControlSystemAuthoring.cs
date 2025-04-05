using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

namespace SparFlame.GamePlaySystem.CameraControl
{
    public class NormalCameraControlSystemAuthoring : MonoBehaviour
    {
        [Header("General")] 
        public bool edgeMoveEnabled;
        

        [Header("Horizontal Translation")] 
        [SerializeField]
        private float translationMaxSpeed = 5f;
        [SerializeField]
        private float translationSpeed = 1f;
        [SerializeField]
        private float translationAcceleration = 10f;
        [SerializeField]
        private float translationDamping = 15f;

        [Header("Vertical Translation")] [SerializeField]
        private float zoomStepSize = 2f;
        [SerializeField]
        private float zoomDampening = 7.5f;
        [SerializeField]
        private float minHeight = 5f;
        [SerializeField]
        private float maxHeight = 50f;
        [SerializeField]
        private float zoomSpeed = 2f;

        [Header("Rotation")] 
        [SerializeField] private float maxRotationSpeed = 1f;
        
        
        [Header("Edge Movement")] [SerializeField] [Range(0f, 0.1f)]
        private float edgeTolerance = 0.05f;
        [SerializeField] private float edgeMovementBaseSpeed = 1.0f;
        [SerializeField] private float speedUpFactor = 2.0f;
        
        
        
        private class CameraControlPlusSystemAuthoringBaker : Baker<NormalCameraControlSystemAuthoring>
        {
            public override void Bake(NormalCameraControlSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new NormalCameraControlConfig
                {
                    TranslationAcceleration = authoring.translationAcceleration,
                    EdgeMoveEnabled = authoring.edgeMoveEnabled,
                    TranslationDamping = authoring.translationDamping,
                    EdgeTolerance = authoring.edgeTolerance,
                    MaxHeight = authoring.maxHeight,
                    MinHeight = authoring.minHeight,
                    ZoomHeightStepSize = authoring.zoomStepSize,
                    ZoomSpeed = authoring.zoomSpeed,
                    MaxRotationSpeed = authoring.maxRotationSpeed,
                    TranslationSpeed = authoring.translationSpeed,
                    TranslationMaxSpeed = authoring.translationMaxSpeed,
                    ZoomDamping = authoring.zoomDampening,
                    EdgeMovementBaseSpeed = authoring.edgeMovementBaseSpeed,
                    SpeedUpFactor = authoring.speedUpFactor,
                });
            }
        }
    }


    public struct CameraFollowTag : IComponentData
    {
        
    }

    public struct NormalCameraControlConfig : IComponentData
    {
        public bool EdgeMoveEnabled;
        public float TranslationSpeed;
        public float TranslationMaxSpeed;
        public float TranslationAcceleration;
        public float TranslationDamping;
        public float ZoomHeightStepSize;
        public float ZoomDamping;
        public float MinHeight;
        public float MaxHeight;
        public float ZoomSpeed;
        public float MaxRotationSpeed;
        public float EdgeTolerance;
        public float EdgeMovementBaseSpeed;
        public float SpeedUpFactor;
    }


}