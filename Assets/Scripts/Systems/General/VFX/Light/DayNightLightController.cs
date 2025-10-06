using Sirenix.OdinInspector;
using SparFlame.Components.General;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.General.VFX
{
    public class DayNightLightController : MonoBehaviour
    {
        [OnValueChanged(nameof(SetLightEditor))] public float settingHour = 12f; // 当前小时

        [Header("Lighting Settings")] public Light sunLight; // Directional Light
        public Gradient lightColorOverDay; // 光颜色随时间变化曲线
        public AnimationCurve lightIntensityOverDay; // 强度随时间变化曲线
        // public Vector3 sunRotationAxis = new Vector3(1f, 0f, 0f); // 太阳旋转轴

        public bool useDefault;

        [ShowIf(nameof(useDefault))] public Color morningColor = new Color(1f, 0.5f, 0.2f);
        [ShowIf(nameof(useDefault))] public Color noonColor = new Color(1f, 1f, 0.9f);
        [ShowIf(nameof(useDefault))] public Color eveningColor = new Color(1f, 0.4f, 0.2f);

        private void Reset()
        {
            sunLight = GetComponent<Light>();
            if (!sunLight)
            {
                Debug.LogWarning("No Light component found on this object.");
            }

            // 默认颜色曲线（黎明-中午-黄昏）
            lightColorOverDay = new Gradient()
            {
                colorKeys = new GradientColorKey[]
                {
                    new GradientColorKey(morningColor, 0.0f), // 清晨橙色
                    new GradientColorKey(noonColor, 0.5f), // 正午白色
                    new GradientColorKey(eveningColor, 1.0f) // 黄昏红橙
                },
                alphaKeys = new GradientAlphaKey[]
                {
                    new GradientAlphaKey(0f, 0.0f),
                    new GradientAlphaKey(1f, 0.3f),
                    new GradientAlphaKey(1f, 0.7f),
                    new GradientAlphaKey(0f, 1.0f)
                }
            };

            // 默认强度曲线（早晚弱，中午强）
            lightIntensityOverDay = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.25f, 0.6f),
                new Keyframe(0.5f, 1f),
                new Keyframe(0.75f, 0.6f),
                new Keyframe(1f, 0f)
            );
        }


        private EntityQuery _timeQuery;

        private void Start()
        {
            _timeQuery = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(typeof(WorldTimeData));
            if (useDefault)
                Reset();
        }

        private void Update()
        {
            if (!sunLight) return;
            if (_timeQuery.IsEmpty) return;
            SetLight();
        }

        private void SetLightEditor()
        {
            var hour = settingHour;
            if (useDefault) Reset();
            var t = hour / TimeUtils.HoursPerDay;
            // 更新光颜色与强度
            sunLight.color = lightColorOverDay.Evaluate(t);
            sunLight.intensity = lightIntensityOverDay.Evaluate(t);

            // 更新光方向（模拟太阳轨迹）
            sunLight.transform.rotation = Quaternion.Euler(t * 360f - 90f, 170f, 0f);
        }
        
        private void SetLight()
        {
            var timeData = _timeQuery.GetSingleton<WorldTimeData>();
            var hour = timeData.hour;
            var t = hour / TimeUtils.HoursPerDay;
            // 更新光颜色与强度
            sunLight.color = lightColorOverDay.Evaluate(t);
            sunLight.intensity = lightIntensityOverDay.Evaluate(t);

            // 更新光方向（模拟太阳轨迹）
            sunLight.transform.rotation = Quaternion.Euler(t * 360f - 90f, 170f, 0f);
        }
    }
}