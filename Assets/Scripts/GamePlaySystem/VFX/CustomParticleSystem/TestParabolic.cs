using UnityEngine;

namespace SparFlame.GamePlaySystem.CustomParticleSystem
{
    using UnityEngine;

    [RequireComponent(typeof(ParticleSystem))]
    public class ParabolicVFXTest : MonoBehaviour
    {
        public Transform target; // 拖动目标点
        public float flightTime = 1.0f; // 粒子飞行时间（秒）

        private ParticleSystem ps;

        void Start()
        {
            ps = GetComponent<ParticleSystem>();
            if (target == null)
            {
                Debug.LogError("Target not assigned.");
                return;
            }

            ConfigureParabola();
            ps.Play();
        }

        void ConfigureParabola()
        {
            Vector3 start = transform.position;
            Vector3 end = target.position;
            Vector3 displacement = end - start;

            float gravity = 9.81f; // Unity 默认重力大小
            float t = flightTime;

            // 计算初速度
            Vector3 velocityXZ = new Vector3(displacement.x, 0, displacement.z) / t;
            float velocityY = (displacement.y + 0.5f * gravity * t * t) / t;

            Vector3 velocity = velocityXZ + Vector3.up * velocityY;

            Debug.Log($"Initial velocity: {velocity}");

            // 设置粒子系统参数
            var main = ps.main;
            main.startLifetime = t;
            main.gravityModifier = 1.0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startSpeed = 0f; // 全部速度由 Velocity over Lifetime 控制

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.Local; // 粒子在 world space 模拟，但 velocity 是 local
            vel.x = new ParticleSystem.MinMaxCurve(velocity.x);
            vel.y = new ParticleSystem.MinMaxCurve(velocity.y);
            vel.z = new ParticleSystem.MinMaxCurve(velocity.z);
        }
    }

}