using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;


namespace SparFlame.System.Cam
{
    public class CameraAuthoring : MonoBehaviour
    {
        public  Camera cam;
        class Baker : Baker<CameraAuthoring>
        {
            public override void Bake(CameraAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new CameraData
                {
                    Position = authoring.cam.transform.position,
                    Rotation = authoring.cam.transform.rotation,
                    ViewMatrix = authoring.cam.worldToCameraMatrix,
                    ProjectionMatrix = authoring.cam.projectionMatrix,
                    ScreenSize = new float2(Screen.width, Screen.height)
                });
            }
        }


    }
    public struct CameraData : IComponentData
    {
        public float3 Position;
        public quaternion Rotation;
        public float4x4 ViewMatrix;
        public float4x4 ProjectionMatrix;
        public float2 ScreenSize;
    }


}