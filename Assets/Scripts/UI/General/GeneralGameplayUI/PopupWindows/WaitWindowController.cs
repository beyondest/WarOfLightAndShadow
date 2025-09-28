using System;
using DG.Tweening;
using SparFlame.Components.General;
using SparFlame.Components.Input;
using SparFlame.Systems.General.BasicControl;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.UI.General
{
    public class WaitWindowController : MonoBehaviour
    {
        [SerializeField] private GameObject panel;

        [SerializeField] private UIInputInteger hour;
        [SerializeField] private UIInputInteger day;
        [SerializeField] private UIInputInteger month;
        [SerializeField] private UIInputInteger year;
        public static WaitWindowController Instance;

        public void Show()
        {
            GeneralModalWindowController.Instance.Show();
            panel.SetActive(true);
        }

        public bool IsOpened()
        {
            return panel.activeSelf;
        }

        public void Hide()
        {
            GeneralModalWindowController.Instance.Hide();
            panel.SetActive(false);
        }

        public void SetEnable(bool enable)
        {
            _enabled = enable;
            if (!enable) Hide();
        }

        public void OnClickPersonalize()
        {
            using var query = World.DefaultGameObjectInjectionWorld.EntityManager
                .CreateEntityQuery(typeof(WorldTimeData));
            var currentWorldTime = query
                .GetSingleton<WorldTimeData>();
            var waitInfo = new WaitInfo
            {
                WaitType = WaitType.Personalize,
                TargetTotalHours = TimeUtils.GetTotalHoursFromWorldTimeData(
                    TimeUtils.GetWaitTargetWorldTime(currentWorldTime,
                        hour.CurrentValue, day.CurrentValue, month.CurrentValue, year.CurrentValue))
            };
            StartCoroutine(GameController.Instance.Wait(waitInfo));
            panel.SetActive(false);
        }

        public void OnClickClock()
        {
            Show();
        }

        public void OnClickWaitUntilBattle()
        {
            Debug.LogWarning("This function is not implemented yet.");
            Hide();
        }

        private EntityQuery _inputQuery;
        private bool _enabled;

        private Vector3 originalPosition; // 初始位置
        private Vector3 originalScale; // 初始大小
        private Tweener currentTween;
        private RectTransform _panelTransform;

        private void Awake()
        {
            if (!Instance)
                Instance = this;
            else
                Destroy(gameObject);
            
        }

        private void Start()
        {
            _inputQuery =
                World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(typeof(InputGeneralShortcutData));
            Hide();
        }

        private void Update()
        {
            if (_inputQuery.IsEmpty) return;
            var generalShortcutData = _inputQuery.GetSingleton<InputGeneralShortcutData>();
            if (generalShortcutData.Wait && _enabled)
            {
                Show();
            }

            if (generalShortcutData.CloseWindow && IsOpened())
            {
                Hide();
            }
        }

        private void OnDestroy()
        {
            if(_inputQuery != default)
                _inputQuery.Dispose();
        }
    }
}