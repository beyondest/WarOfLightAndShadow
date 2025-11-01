using System;
using System.Collections;
using SparFlame.Components.General;
using SparFlame.UI.General;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

// ReSharper disable PossibleNullReferenceException

namespace SparFlame.UI.SubGameplay
{
    public class UnitMulti2DWindow : MultiSlotWindowUtils.MultiSlotsWindow<Unit2DSlot>
    {
        // Config
        [Header("Custom Config")] [SerializeField]
        private GameObject pageUpButton;
        [SerializeField] private Tier maxTier;
        [SerializeField] private GameObject pageDownButton;

        #region Interface
        

        public static UnitMulti2DWindow Instance;
        public event Action<int> OnGetTargetEntityByIndex;
        public event Action<Entity> OnDeselectAllExceptOne;

        public void DisableClickRoutine()
        {
            _ifClickRoutineRunning = true;
        }

        public void EnableClickRoutine()
        {
            _ifClickRoutineRunning = false;
            _clickCount = 0;
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

        public void GetUnitData(int curSelectCount, Entity target)
        {
            _currentSelectCounts = curSelectCount;
            _targetEntity = target;
            
        }
        public void UpdateSelectedUnitView(NativeList<UnitRealTimeInfo> unitInfos, FactionTag faction)
        {
            _currentSelectFaction = faction;
            var startIdx = _currentPage * _slotsMaxCountPerPage;
            var count = Mathf.Min(_slotsMaxCountPerPage, unitInfos.Length - startIdx);
            // Update corresponding images and hp sliders
            for (var i = 0; i < _slotsMaxCountPerPage; i++)
            {
                if (i < count)
                {
                    Slots[i].SetActive(true);
                    var unitShowSlot = SlotComponents[i];
                    var unitInfo = unitInfos[startIdx + i];
                    unitShowSlot.SetTarget(unitInfo, _currentSelectFaction,
                        _maxTierF
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
            pageDownButton.SetActive((_currentPage + 1) * _slotsMaxCountPerPage < unitInfos.Length);
            pageUpButton.SetActive(_currentPage != 0);
            _currentSelectCounts = unitInfos.Length;
        }

        public override void LoadResources()
        {
            base.LoadResources();
            _slotsMaxCountPerPage = config.rows * config.cols;
            _currentSelectIndex = -1;
            _maxTierF = (int)maxTier - 2;
        }
        
        #endregion

        #region ButtonMethods
        public void OnPageRightClicked()
        {
            _currentPage++;
        }

        public void OnPageLeftClicked()
        {
            _currentPage--;
        }
        public override void OnClickSlot(int slotIndex)
        {
            _clickCount++;
            if(_ifClickRoutineRunning)return;
            
            var trueIndex = _currentPage * _slotsMaxCountPerPage + slotIndex;
            if (_currentSelectIndex == trueIndex) return;
            // Set close up target 
            OnGetTargetEntityByIndex?.Invoke(trueIndex);
            if (_currentSelectCounts <= trueIndex) return;
            InfoWindowController.Instance.UpdateCloseUpTarget(_targetEntity);
            UnitDetailWindow.Instance.TrySwitchTarget(_targetEntity);
            StartCoroutine(ClickRoutine());
        }

        #endregion

        
        // Internal Data
        private int _slotsMaxCountPerPage;
        private int _currentPage;
        private int _currentSelectIndex = -1;
        private int _currentSelectCounts;
        private float _maxTierF;
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
            World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<UnitMulti2DWindowSystem>()
                .Init(this);
        }

        #endregion

        private IEnumerator ClickRoutine()
        {
            _ifClickRoutineRunning = true;
            yield return new WaitForSecondsRealtime(GlobalUIConfigger.Instance.doubleClickThreshold);
            if (_clickCount == 1)
            {
                UnitDetailWindow.Instance.Show();
                UnitDetailWindow.Instance.ShowReturnButton();
                InteractAbilityWindow.Instance.Show();
                
            }
            else if (_clickCount >= 2)
            {
                UnitDetailWindow.Instance.Show();
                InteractAbilityWindow.Instance.Show();
                Hide();
                OnDeselectAllExceptOne?.Invoke(_targetEntity);
            }
            _ifClickRoutineRunning = false;
            _clickCount = 0;
        }
    }
}