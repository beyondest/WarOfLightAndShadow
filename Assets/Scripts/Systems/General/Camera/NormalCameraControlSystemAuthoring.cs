using System;
using SparFlame.Components.MainGameplay;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SparFlame.Systems.General.Camera
{
    public class NormalCameraControlSystemAuthoring : MonoBehaviour
    {
        [Header("Main Game Camera")] public NormalCameraControlConfig config;
        [SerializeField] private float3 cameraStartPosLight;
        [SerializeField] private float3 cameraStartPosDark;
        [Header("Sub Game Camera")] public NormalCameraControlConfig subConfig;
        [SerializeField] private float3 subGameplayRigStartPos;
        [SerializeField] private float3 subGameplayCameraStartLocalPos;
        
        
        private class CameraControlPlusSystemAuthoringBaker : Baker<NormalCameraControlSystemAuthoring>
        {
            public override void Bake(NormalCameraControlSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, authoring.config);
                AddComponent<MainGameCameraTag>(entity);
                AddComponent(entity, new CameraStartPosData
                {
                    LightInitStartPos = authoring.cameraStartPosLight,
                    DarkInitStartPos = authoring.cameraStartPosDark,
                    InitSubGameplayRigPosition = authoring.subGameplayRigStartPos,
                    InitSubGameplayCameraLocalPosition = authoring.subGameplayCameraStartLocalPos
                });
                
                
                var subEntity = CreateAdditionalEntity(TransformUsageFlags.None);
                AddComponent(subEntity, authoring.subConfig);
                AddComponent<SubGameCameraTag>(subEntity);
                AddComponent<MiniMapControlData>(subEntity);
                AddComponent<DraggingTag>(subEntity);
                SetComponentEnabled<DraggingTag>(subEntity,false);
                
            }
        }
    }




    [Serializable]
    public struct NormalCameraControlConfig : IComponentData
    {
        public bool edgeMoveEnabled;
        public float limitPosBias;
        
        [Header("Horizontal Translation")] 
        public float speedForTargetMoving;
        public float speedForWasd;
        public float translationMaxSpeed;
        public float translationAcceleration;
        public float translationDamping;
        
        [Header("Vertical Translation")] [SerializeField]
        public float zoomHeightStepSize;
        public float zoomDamping;
        public float minHeight;
        public float maxHeight;
        public float zoomSpeed;
        
        [Header("Rotation")] 
        public float maxRotationSpeed;
        
        [Header("Edge Movement")] [SerializeField] [Range(0f, 0.1f)]
        public float edgeTolerance;
        public float edgeMovementBaseSpeed;
        
        [Header("Speed Up")]
        public float speedUpFactor;
    }

    


}