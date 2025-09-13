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
        private EntityQuery _inputQuery;
        
        public void Show()
        {
            panel.SetActive(true);
        }

        public void Hide()
        {
            panel.SetActive(false);
        }

        public void OnClickPersonalize()
        {
            var timeConfig = World.DefaultGameObjectInjectionWorld.EntityManager
                .CreateEntityQuery(typeof(GameTimeConfig))
                .GetSingleton<GameTimeConfig>();
            var timeScale = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(typeof(GameTimeScale))
                .GetSingletonRW<GameTimeScale>();
            timeScale.ValueRW.Value = timeConfig.waitTimeScale;
            
            var waitInfo = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(typeof(WaitInfo))
                .GetSingletonRW<WaitInfo>();
            var currentWorldTime = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(typeof(WorldTimeData))
                .GetSingleton<WorldTimeData>();
            
            waitInfo.ValueRW.WaitType = WaitType.Personalize;

            waitInfo.ValueRW.TargetTotalHours = TimeUtils.GetTotalHoursFromWorldTimeData(
                TimeUtils.GetWaitTargetWorldTime(currentWorldTime,
                    hour.CurrentValue, day.CurrentValue, month.CurrentValue, year.CurrentValue));
            Hide();
        }

        public void OnClickClock()
        {
            Show();
        }

        public void OnClickWaitUntilBattle()
        {
            Hide();
        }

        private void Start()
        {
            _inputQuery = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(typeof(InputGeneralShortcutData));
            Hide();
        }

        private void Update()
        {
            if(_inputQuery.IsEmpty)return;
            var generalShortcutData = _inputQuery.GetSingleton<InputGeneralShortcutData>();
            if (generalShortcutData.Wait)
            {
                Show();
            }

            if (generalShortcutData.CloseWindow)
            {
                Hide();
            }
        }
    }
}