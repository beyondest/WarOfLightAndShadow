using System;
using System.Collections;
using SparFlame.Core.Utils;
using SparFlame.Systems.General.BasicControl;
using TMPro;
using UnityEngine;

namespace SparFlame.UI.General
{
    public class SaveLoadMenu : MultiSlotWindowUtils.MultiSlotsWindow<SaveSlot>
    {
        [SerializeField] private GameObject window;
        [SerializeField] private TMP_Text title;
        [SerializeField] private float checkInterval = 0.1f;
        public static SaveLoadMenu Instance;
        public void SetModeAndShow(WindowMode mode)
        {
            _currentMode = mode;
            title.text = mode switch
            {
                WindowMode.Save => "Save Menu",
                _ => "Load Menu",
            };
            Show();
            var latestTime = DateTime.MinValue;
            var pre = _latestSaveSlot;
            _latestSaveSlot = SlotComponents[0];
            foreach (var slot in SlotComponents)
            {
                var writeTime = slot.GetLastSaveTime();
                if (writeTime <= latestTime) continue;
                latestTime = writeTime;
                _latestSaveSlot = slot;
            }
            pre?.SetLatestBorderEnable(false);
            _latestSaveSlot.SetLatestBorderEnable(true);
        }

        public override void Show(Vector2? pos = null)
        {
            window.SetActive(true);
        }

        public override void Hide()
        {
            window.SetActive(false);
        }

        #region Button Methods

        public void OnClickBack()
        {
            Hide();
        }


        public override void OnClickSlot(int slotIndex)
        {
            var slot = SlotComponents[slotIndex];
            switch (_currentMode)
            {
                case WindowMode.LoadInGame:
                    slot.TryLoadSlot(false);
                    break;
                case WindowMode.Save:
                    slot.TrySaveSlot();
                    break;
                case WindowMode.LoadInMainMenu:
                    slot.TryLoadSlot(true);
                    break;
                default:
                    BurstSafe.UnexpectedEnum(_currentMode);
                    break;
            }
        }

        #endregion

        public enum WindowMode
        {
            LoadInGame,
            LoadInMainMenu,
            Save,
        }

        private WindowMode _currentMode;
        private SaveSlot _latestSaveSlot;

        private void Awake()
        {
            if (!Instance)
                Instance = this;
            else
                Destroy(gameObject);
        }

        protected override void Start()
        {
            Hide();
            LoadResources();
            StartCoroutine(CheckLoadCompleteAndInit());
        }

        private void OnDestroy()
        {
            UnloadResources();
        }

        private IEnumerator CheckLoadCompleteAndInit()
        {
            while (Slots.Count != config.rows * config.cols )
            {
                yield return new WaitForSecondsRealtime(checkInterval);
            }
            for (var i = 0; i < SlotComponents.Count; i++)
            {
                var slot = SlotComponents[i];
                slot.SetIndex(i);
            }
            SlotComponents[0].SetIndex(SaveUtilities.AutomaticSaveSlot);
        }
    }
}