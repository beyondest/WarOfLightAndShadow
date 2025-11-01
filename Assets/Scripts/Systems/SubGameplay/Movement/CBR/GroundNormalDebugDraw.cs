using SparFlame.Components.SubGameplay;
using SparFlame.Systems.SubGameplay.Movement.CBR.Authoring;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace SparFlame.Systems.SubGameplay.Movement
{
    public partial class DebugDrawNormalsSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<DebugDrawNormalVector>();
        }

        protected override void OnUpdate()
        {
            const float length = 1.0f; // Normal vector length
            const float headLen = 0.2f; // Arrow head length
            const float headWidth = 0.08f; // Arrow head width
            foreach (var (trans, groundInfo) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<GroundInfo>>())
            {
                var p = trans.ValueRO.Position;
                var n = groundInfo.ValueRO.Normal;
                if (math.lengthsq(n) < 1e-6f) return;
                var nn = math.normalize(n);

                Debug.DrawLine(p, p + nn * length, Color.green);

                var right = math.normalize(math.cross(nn, new float3(0.001f, 1f, 0.001f)));
                if (math.lengthsq(right) < 1e-6f) right = new float3(1, 0, 0);
                var up = math.normalize(math.cross(right, nn));

                Debug.DrawLine(p + nn * length,
                    p + nn * (length - headLen) + (up + right) * headWidth,
                    Color.green);

                Debug.DrawLine(p + nn * length,
                    p + nn * (length - headLen) + (up - right) * headWidth,
                    Color.green);
            }
        }
    }
}