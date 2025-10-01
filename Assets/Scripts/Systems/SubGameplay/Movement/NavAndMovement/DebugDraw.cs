using SparFlame.Components.SubGameplay;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace SparFlame.Systems.SubGameplay.Movement
{
    // DebugDrawNormalsSystem.cs

 

// 把这个 System 挂到主线程执行（Run），用来在 Scene 视图实时画线
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class DebugDrawNormalsSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            float length = 1.0f; // 法线可视长度
            float headLen = 0.2f; // 箭头长度
            float headWidth = 0.08f; // 箭头宽度

            Entities
                .ForEach((in LocalTransform pos, in GroundInfo info) =>
                {
                    float3 p = pos.Position;
                    float3 n = info.Normal;
                    if (math.lengthsq(n) < 1e-6f) return;
                    float3 nn = math.normalize(n);

                    // 主线
                    Debug.DrawLine(p, p + nn * length, Color.green);

                    // 箭头（两个小线段）
                    float3 right = math.normalize(math.cross(nn, new float3(0.001f, 1f, 0.001f)));
                    if (math.lengthsq(right) < 1e-6f) right = new float3(1, 0, 0);
                    float3 up = math.normalize(math.cross(right, nn));

                    Debug.DrawLine(p + nn * length,
                        p + nn * (length - headLen) + (up + right) * headWidth,
                        Color.green);

                    Debug.DrawLine(p + nn * length,
                        p + nn * (length - headLen) + (up - right) * headWidth,
                        Color.green);
                })
                .WithoutBurst() // Debug.DrawLine 是 UnityEngine API，不能 Burst
                .Run();
        }
    }
}