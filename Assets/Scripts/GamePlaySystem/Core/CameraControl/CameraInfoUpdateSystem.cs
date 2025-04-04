using SparFlame.GamePlaySystem.General;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SparFlame.GamePlaySystem.CameraControl
{
    [UpdateAfter(typeof(CameraControlPlusSystem))]
    public partial class CameraInfoUpdateSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<NotPauseTag>();
            RequireForUpdate<CameraData>();
        }

        protected override void OnUpdate()
        {
            if(Camera.main == null)return;
            var camera = Camera.main;
            var cameraData = SystemAPI.GetSingletonRW<CameraData>();
            UpdateCameraData(ref cameraData.ValueRW, camera);
        }
        private void UpdateCameraData(ref CameraData cameraData, Camera cam)
        {
            cameraData.ViewMatrix = cam.worldToCameraMatrix;
            cameraData.ProjectionMatrix = cam.projectionMatrix;
            cameraData.ScreenSize = new float2(Screen.width, Screen.height);
        }
    }
}