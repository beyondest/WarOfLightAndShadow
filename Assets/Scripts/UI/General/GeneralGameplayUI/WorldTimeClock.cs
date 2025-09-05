using System;
using SparFlame.Components.General;
using UnityEngine;
using TMPro;
using Unity.Entities;

public class WorldTimeClock : MonoBehaviour
{

    
    [Header("Clock Hands")]
    [SerializeField]private RectTransform hourHand;
    [SerializeField] private RectTransform minuteHand;

    [Header("Text")] public TMP_Text yearText;
    [SerializeField] private TMP_Text monthText;
    [SerializeField] private TMP_Text dayText;
    [SerializeField] private TMP_Text pmAmText; 
    private readonly string[] months = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

    private EntityQuery _worldTime;

    private void Start()
    {
        _worldTime = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(typeof(WorldTimeData));
    }

    private void Update()
    {
        if(_worldTime.IsEmpty)return;

        var worldTime = _worldTime.GetSingleton<WorldTimeData>();
        UpdateClock(worldTime);
    }

    public void UpdateClock(WorldTimeData data)
    {
        // 1. 时针分针
        // hour: [0, 24)，minute = hour的小数部分
        var totalMinutes = data.hour * 60f;
        var hours = Mathf.Floor(totalMinutes / 60f);
        var minutes = totalMinutes % 60f;

        // 每小时 = 30°，每分钟 = 6°
        var hourAngle = (hours % 12 + minutes / 60f) * 30f;
        var minuteAngle = minutes * 6f;

        if (hours > 12)
        {
            pmAmText.text = "P.M.";
        }
        else
        {
            pmAmText.text = "A.M.";
        }
        
        if (hourHand)
            hourHand.localRotation = Quaternion.Euler(0, 0, -hourAngle);

        if (minuteHand)
            minuteHand.localRotation = Quaternion.Euler(0, 0, -minuteAngle);

        // 2. 年份 (弧形文字)
        if (yearText)
            yearText.text = data.year.ToString();

        // 3. 月份缩写

        if (monthText)
            monthText.text = months[Mathf.Clamp(data.month - 1, 0, 11)];

        // 4. 天数
        if (dayText)
            dayText.text = $"{data.day}";
    }
}