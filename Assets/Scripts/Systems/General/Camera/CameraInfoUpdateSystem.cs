using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SparFlame.Systems.General.Camera
{
    public partial class CameraInfoUpdateSystem : SystemBase
    {
        private UnityEngine.Camera _mainCamera;
        private bool _initialized;
        protected override void OnCreate()
        {
            RequireForUpdate<GameStatusData>();
            RequireForUpdate<CameraData>();
            RequireForUpdate<WaitInfo>();
        }

     
        protected override void OnUpdate()
        {
            var gameStatus = SystemAPI.GetSingleton<GameStatusData>().Value;
            if(gameStatus != GameStatus.MainGaming && gameStatus!= GameStatus.SubGaming)return;
            var waitInfo = SystemAPI.GetSingleton<WaitInfo>();
            if(waitInfo.WaitType != WaitType.None)return;
            
            // _mainCamera = gameStatus == GameStatus.MainGaming ? CameraController.Instance.mainGameCamera : CameraController.Instance.subGameCamera;
            _mainCamera = UnityEngine.Camera.main;
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
        }
    }
}