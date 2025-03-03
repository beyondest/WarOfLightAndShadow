using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SparFlame.System.Cam
{
    public partial class CameraSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var camera = Camera.main;
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var cameraEntity = SystemAPI.GetSingletonEntity<CameraData>();

            if (camera != null)
                entityManager.SetComponentData(cameraEntity, new CameraData
                {
                    Position = camera.transform.position,
                    Rotation = camera.transform.rotation,
                    ViewMatrix = camera.worldToCameraMatrix,
                    ProjectionMatrix = camera.projectionMatrix,
                    ScreenSize = new float2(Screen.width, Screen.height)
                });
        }
    }



}