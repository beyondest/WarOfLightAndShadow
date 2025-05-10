using SparFlame.GamePlaySystem.General;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SparFlame.GamePlaySystem.CameraControl
{
    public partial class CameraInfoUpdateSystem : SystemBase
    {
        private Camera _mainCamera;
        private bool _initialized;
        protected override void OnCreate()
        {
            RequireForUpdate<GamingTag>();
            RequireForUpdate<CameraData>();
        }

        protected override void OnStartRunning()
        {
            _mainCamera = Camera.main;
        }
        protected override void OnUpdate()
        {
            var cameraData = SystemAPI.GetSingletonRW<CameraData>();
            UpdateCameraData(ref cameraData.ValueRW);
        }
        private void UpdateCameraData(ref CameraData cameraData)
        {
            cameraData.ViewMatrix = _mainCamera.worldToCameraMatrix;
            cameraData.ProjectionMatrix = _mainCamera.projectionMatrix;
            cameraData.ScreenSize = new float2(Screen.width, Screen.height);
            cameraData.CameraRight = _mainCamera.transform.right;
            cameraData.CameraForward = _mainCamera.transform.forward;
            cameraData.CameraUp = _mainCamera.transform.up;
            cameraData.CameraPosition = _mainCamera.transform.position;
        }
    }
}