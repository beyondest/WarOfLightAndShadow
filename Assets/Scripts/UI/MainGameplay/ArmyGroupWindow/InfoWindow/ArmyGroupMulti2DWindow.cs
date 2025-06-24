using System;
using System.Collections;
using SparFlame.Components.General;
using SparFlame.UI.General;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.UI.MainGameplay
{
    public class ArmyGroupMulti2DWindow : MultiSlotWindowUtils.MultiSlotsWindow<ArmyGroupMulti2DSlot>
    {

        // Config
        [Header("Custom Config")] [SerializeField]
        private GameObject pageUpButton;

        [SerializeField] private GameObject pageDownButton;
        

        // Interface
        public static ArmyGroupMulti2DWindow Instance;
        public Action<int> GetTargetEntityByIndex;
        public Action<Entity> DeselectAllExceptOne;

        public void DisableClickRoutine()
        {
            _ifClickRoutineRunning = true;
        }

        public void EnableClickRoutine()
        {
            _ifClickRoutineRunning = false;
            _clickCount = 0;
        }

        public override void OnClickSlot(int slotIndex)
        {
            _clickCount++;
            if(_ifClickRoutineRunning)return;
            
            var trueIndex = _currentPage * _slotsMaxCountPerPage + slotIndex;
            if (_currentSelectIndex == trueIndex) return;
            // Set close up target 
            GetTargetEntityByIndex?.Invoke(trueIndex);
            if (_currentSelectCounts <= trueIndex) return;
            MainGameplayInfoWindowController.Instance.UpdateCloseUpTarget(_targetEntity);
            ArmyGroupDetailWindow.Instance.TrySwitchTarget(_targetEntity);
            StartCoroutine(ClickRoutine());
        }


   
        public bool HasTarget()
        {
            return _currentSelectCounts > 0;
        }

  
        public override void Hide()
        {
            base.Hide();
            _currentSelectIndex = -1;
        }

        public void GetSelectionData(int curSelectCount, Entity target)
        {
            _currentSelectCounts = curSelectCount;
            _targetEntity = target;
        }
        public override void LoadResources()
        {
            base.LoadResources();
            _slotsMaxCountPerPage = config.rows * config.cols;
            _currentSelectIndex = -1;
        }
        
        public void UpdateSelectedView(NativeList<ArmyGroupMulti2DRealTimeInfo> infos, FactionTag faction)
        {
            _currentSelectFaction = faction;
            var startIdx = _currentPage * _slotsMaxCountPerPage;
            var count = Mathf.Min(_slotsMaxCountPerPage, infos.Length - startIdx);
            // Update corresponding images and hp sliders
            for (var i = 0; i < _slotsMaxCountPerPage; i++)
            {
                if (i < count)
                {
                    Slots[i].SetActive(true);
                    var slotComponent = SlotComponents[i];
                    var info = infos[startIdx + i];
                    slotComponent.SetTarget(info, _currentSelectFaction
                        );
                    // unitShowSlot.button.image.sprite =
                    //     UnitWindowResourceManager.Instance.UnitGeneralTypeSprites[unitInfo.UnitType];
                    // unitShowSlot.hpFilled.fillAmount = unitInfo.HpRatio;
                    // var tier = (int)unitInfo.Tier - 2;
                    // unitShowSlot.tierImage.fillAmount = tier / _maxTierF;
                }
                else
                {
                    Slots[i].SetActive(false);
                }
            }

            // Update right and left button
            pageDownButton.SetActive((_currentPage + 1) * _slotsMaxCountPerPage < infos.Length);
            pageUpButton.SetActive(_currentPage != 0);
            _currentSelectCounts = infos.Length;
        }


        #region ButtonMethods
        public void OnPageRightClicked()
        {
            _currentPage++;
        }

        public void OnPageLeftClicked()
        {
            _currentPage--;
        }

        #endregion

        
        // Internal Data
        private int _slotsMaxCountPerPage;
        private int _currentPage;
        private int _currentSelectIndex = -1;
        private int _currentSelectCounts;
        private FactionTag _currentSelectFaction;
        private Entity _targetEntity;
        private int _clickCount;
        private bool _ifClickRoutineRunning;
        
        #region EventFunction

        private void Awake()
        {
            if(!Instance)
                Instance = this;
            else
                Destroy(gameObject);
        }

        protected override void Start()
        {
            base.Start();
            Hide();
        }

        #endregion

        private IEnumerator ClickRoutine()
        {
            _ifClickRoutineRunning = true;
            yield return new WaitForSeconds(GlobalUIDoubleClicker.Instance.doubleClickThreshold);
            if (_clickCount == 1)
            {
                ArmyGroupDetailWindow.Instance.Show();
            }
            else if (_clickCount >= 2)
            {
                ArmyGroupDetailWindow.Instance.Show();
                Hide();
                DeselectAllExceptOne?.Invoke(_targetEntity);
            }
            _ifClickRoutineRunning = false;
            _clickCount = 0;
        }
    }
}