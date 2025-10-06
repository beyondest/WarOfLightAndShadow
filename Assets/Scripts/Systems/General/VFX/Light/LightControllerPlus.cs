using Sirenix.OdinInspector;
using SparFlame.Components.General;
using Unity.Entities;

namespace SparFlame.Systems.General.VFX
{
    using UnityEngine;

    public class LightControllerPlus : MonoBehaviour
    {
        [OnValueChanged(nameof(SetLightEditor))] 
        public float settingHour = 12f; // 当前小时（0–24）

    
        [Header("Light Sources")] public Light sunLight; // 太阳光（Directional Light）
        public Light moonLight; // 月亮光（Directional Light）

        [Header("Sun Settings")] public Gradient sunColorOverDay;
        public AnimationCurve sunIntensityOverDay;

        [Header("Moon Settings")] public Gradient moonColorOverNight;
        public AnimationCurve moonIntensityOverNight;

        [Header("Rotation Settings")] public float sunBaseRotationY = 170f; // 太阳水平偏转角
        public Vector3 rotationAxis = Vector3.right; // 太阳旋转轴（绕X轴旋转）
        public bool ifUseDefault;
        private void Reset()
        {
            if (!sunLight) Debug.LogWarning("未分配太阳光（sunLight）。");
            if (!moonLight) Debug.LogWarning("未分配月亮光（moonLight）。");

            // 默认太阳颜色曲线
            sunColorOverDay = new Gradient()
            {
                colorKeys = new GradientColorKey[]
                {
                    new(new Color(1f, 0.45f, 0.2f), 0.0f), // 清晨橙
                    new(new Color(1f, 1f, 0.9f), 0.5f), // 正午白
                    new(new Color(1f, 0.4f, 0.2f), 1.0f) // 傍晚红
                }
            };

            sunIntensityOverDay = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.25f, 0.6f),
                new Keyframe(0.5f, 1f),
                new Keyframe(0.75f, 0.6f),
                new Keyframe(1f, 0f)
            );

            // 默认月亮曲线（夜间冷光）
            moonColorOverNight = new Gradient()
            {
                colorKeys = new GradientColorKey[]
                {
                    new(new Color(0.4f, 0.5f, 1f), 0.0f),
                    new(new Color(0.6f, 0.7f, 1f), 0.5f),
                    new(new Color(0.4f, 0.5f, 1f), 1.0f)
                }
            };

            moonIntensityOverNight = new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(0.5f, 0.5f),
                new Keyframe(1f, 1f)
            );
        }


        private void SetLightEditor()
        {
            SetLight(settingHour, 24f);
        }

        private EntityQuery _time;
        
        private void Start()
        {
            if(ifUseDefault)Reset();
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            _time = em.CreateEntityQuery(typeof(WorldTimeData));
        }

        private void Update()
        {
            if (!sunLight || !moonLight)
                return;
            if(_time.IsEmpty)return;
            var timeData = _time.GetSingleton<WorldTimeData>();
            SetLight(timeData.hour, TimeUtils.HoursPerDay );
          
        }

        private void SetLight(float hour, float hoursPerDay)
        {
            if (hour >= hoursPerDay)
                hour -= hoursPerDay;

            float t = hour / hoursPerDay; // [0,1] 映射一整天

            // 🌞 太阳角度（绕X旋转一圈）
            float sunAngle = t * 360f - 90f;
            sunLight.transform.rotation =
                Quaternion.Euler(rotationAxis * sunAngle) * Quaternion.Euler(0, sunBaseRotationY, 0);

            // 🌙 月亮与太阳相对（相差180°）
            moonLight.transform.rotation = Quaternion.Euler(rotationAxis * (sunAngle + 180f)) *
                                           Quaternion.Euler(0, sunBaseRotationY, 0);

            // 🌞 白天比例（白天亮、夜晚暗）
            float dayFactor = Mathf.Clamp01(Mathf.Cos(t * Mathf.PI * 2f) * 0.5f + 0.5f); // 白天为1，夜晚为0
            float nightFactor = 1f - dayFactor;

            // 🌞 更新太阳光
            sunLight.color = sunColorOverDay.Evaluate(t);
            sunLight.intensity = sunIntensityOverDay.Evaluate(dayFactor);

            // 🌙 更新月亮光
            moonLight.color = moonColorOverNight.Evaluate(t);
            moonLight.intensity = moonIntensityOverNight.Evaluate(nightFactor);

            // 平滑淡入淡出，避免切换闪烁
            sunLight.enabled = sunLight.intensity > 0.05f;
            moonLight.enabled = moonLight.intensity > 0.05f;
        }
    }
}