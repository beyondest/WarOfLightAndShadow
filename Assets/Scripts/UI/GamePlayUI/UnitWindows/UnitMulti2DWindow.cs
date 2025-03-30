using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Interact;
using SparFlame.GamePlaySystem.Units;
using SparFlame.GamePlaySystem.UnitSelection;
using SparFlame.UI.General;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

// ReSharper disable PossibleNullReferenceException

namespace SparFlame.UI.GamePlay
{
    public class UnitMulti2DWindow : UIWindow
    {
        public static UnitMulti2DWindow Instance;

        [Header("Custom Config")] public GameObject multiUnitPanel;
        public GameObject pageUpButton;
        public GameObject pageDownButton;

        [Tooltip("If Melee unit 2D sprite located in Assets/Resources/UI/Unit2DSprites/Unit2DMelee, " +
                 "then path should be UI/Unit2DSprites, prefix should be Unit2D")]
        public string unitType2DSpritePath = "UI/Unit2DSprites/";
        [CanBeNull] public string prefix = "Unit2D";

        [Header("Multi unit slot config")] public GameObject slotPrefab;
        public MultiShowSlotConfig multiShowSlotConfig;

        public override void Show(Vector2? pos = null)
        {
            multiUnitPanel.SetActive(true);
        }

        public override void Hide()
        {
            multiUnitPanel.SetActive(false);
            _currentSelectIndex = -1;
        }

        public override bool IsOpened()
        {
            return multiUnitPanel.activeSelf;
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

        public void OnClickUnit2D(int index)
        {
            if (_unitInfos.Count <= index) return;
            if (_currentSelectIndex == index) return;
            // Set close up target 
            InfoWindowController.Instance.UpdateCloseUpTarget(_unitInfos[index + _currentPage * _slotsMaxCountPerPage].Entity);
        }

        #endregion


        private int _slotsMaxCountPerPage;
        private int _currentPage;
        private int _currentSelectIndex = -1;
        private List<GameObject> _slots = new();
        private Dictionary<UnitType, Sprite> _unitSpriteDict = new();
        private readonly List<UnitInfo> _unitInfos = new();

        private EntityManager _em;
        private EntityQuery _selectedQuery;
        private EntityQuery _notPauseTag;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private void Start()
        {
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            _notPauseTag = _em.CreateEntityQuery(typeof(NotPauseTag));
            _slotsMaxCountPerPage = multiShowSlotConfig.rows * multiShowSlotConfig.cols;
            UIUtils.InitMultiShowSlots(ref _slots, multiUnitPanel, slotPrefab, in multiShowSlotConfig, OnClickUnit2D);
            _unitSpriteDict = UIUtils.LoadTypeSprites<UnitType>(unitType2DSpritePath, prefix);
            Hide();
        }

        private void Update()
        {
            if (_notPauseTag.IsEmpty) return;
            if (!IsOpened()) return;
            _selectedQuery = _em.CreateEntityQuery(new EntityQueryBuilder(Allocator.Temp)
                .WithAll<Selected>().WithAll<StatData>().WithAll<UnitAttr>());
            _unitInfos.Clear();
            var entities = _selectedQuery.ToEntityArray(Allocator.Temp);
            var unitBasicAttrs = _selectedQuery.ToComponentDataArray<UnitAttr>(Allocator.Temp);
            var stats = _selectedQuery.ToComponentDataArray<StatData>(Allocator.Temp);
            for (var i = 0; i < stats.Length; i++)
            {
                _unitInfos.Add(new UnitInfo
                {
                    UnitType = unitBasicAttrs[i].Type,
                    HpRatio = 1 - (float)stats[i].CurValue / stats[i].MaxValue,
                    Entity = entities[i],
                });
            }

            UpdateSelectedUnitView(in _unitInfos);
        }


        private void UpdateSelectedUnitView(in List<UnitInfo> selectedUnitInfo)
        {
            var startIdx = _currentPage * _slotsMaxCountPerPage;
            var count = Mathf.Min(_slotsMaxCountPerPage, selectedUnitInfo.Count - startIdx);
            // Update corresponding images and hp sliders
            for (var i = 0; i < _slotsMaxCountPerPage; i++)
            {
                if (i < count)
                {
                    _slots[i].SetActive(true);
                    var unitShowSlot = _slots[i].GetComponent<UnitMulti2DSlot>();
                    var unitInfo = selectedUnitInfo[startIdx + i];
                    unitShowSlot.button.image.sprite = _unitSpriteDict[unitInfo.UnitType];
                    unitShowSlot.hp.value = unitInfo.HpRatio;
                }
                else
                {
                    _slots[i].SetActive(false);
                }
            }

            // Update right and left button
            pageDownButton.SetActive((_currentPage + 1) * _slotsMaxCountPerPage < selectedUnitInfo.Count);
            pageUpButton.SetActive(_currentPage != 0);
        }


        private struct UnitInfo
        {
            public UnitType UnitType;
            public float HpRatio;
            public Entity Entity;
        }
    }
}