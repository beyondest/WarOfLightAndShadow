using System;
using System.Collections.Generic;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Interact;
using SparFlame.GamePlaySystem.Units;
using SparFlame.GamePlaySystem.UnitSelection;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Serialization;

namespace SparFlame.UI.GamePlay.UI.GamePlayUI
{
    public class MultiUnitShowController : MonoBehaviour
    {
        public static MultiUnitShowController Instacne;
        public List<UnitTypeSpritePair> unitTypeImagePairs;
        public int rows = 2;
        public int cols = 10;
        public GameObject multiUnitPanel;
        public GameObject pageUpButton;
        public GameObject pageDownButton;
        public GameObject slotPrefab;
        public Vector2 startPos;
        public float columnSpacing;
        public float rowSpacing;
        public Entity forCloseUpShowUnit;
        
        
        public void ShowMultiUnitPanel()
        {
            multiUnitPanel.SetActive(true);
        }

        public void HideMultiUnitPanel()
        {
            multiUnitPanel.SetActive(false);
        }

        private void OnClickSlotImage(int index)
        {
            if(_unitInfos.Count <= index)return;
            var info = _unitInfos[index];
            forCloseUpShowUnit = info.Entity;
            Debug.Log($"click on {info.Entity}");
        }

        
        private int _slotsMaxCountPerPage;
        private int _currentPage = 0;
        private readonly List<GameObject> _slots = new();
        private readonly Dictionary<UnitType, Sprite> _unitSpriteDict = new();
        private EntityManager _em;
        private EntityQuery _entityQuery;
        private List<UnitInfo> _unitInfos = new();
        private void Awake()
        {
            if (Instacne == null)
            {
                Instacne = this;
                // DontDestroyOnLoad(gameObject);
            }
            else
                Destroy(gameObject);
        }

        private void Start()
        {
            _slotsMaxCountPerPage = rows * cols;
            foreach (var pair in unitTypeImagePairs)
            {
                _unitSpriteDict.Add(pair.unitType, pair.sprite);
            }
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            _entityQuery = _em.CreateEntityQuery(new EntityQueryBuilder(Allocator.Temp)
                .WithAll<Selected>().WithAll<StatData>().WithAll<UnitBasicAttr>());
            InitMultiUnitShowSlots();
            multiUnitPanel.SetActive(false);
        }

        
        private void Update()
        {
            if (!_em.CreateEntityQuery(typeof(NotPauseTag)).TryGetSingletonEntity< NotPauseTag>(out var _))return;
            // get first selected unit for close up show by default
            var entities = _entityQuery.ToEntityArray(Allocator.Temp);
            var unitBasicAttrs = _entityQuery.ToComponentDataArray<UnitBasicAttr>(Allocator.Temp);
            var stats = _entityQuery.ToComponentDataArray<StatData>(Allocator.Temp);
            _unitInfos.Clear();
            if (entities.Length <= 1)
            {
                // Record the first entity for close up show by default
                forCloseUpShowUnit = entities.Length > 0 ? entities[0] : Entity.Null;
                // when not multi select, disable the slots by using update blank list
                UpdateSelectedUnitView(_unitInfos);
                return;
            };
            for (var i = 0; i < stats.Length; i++)
            {
                _unitInfos.Add(new UnitInfo
                {
                    UnitType   = unitBasicAttrs[i].UnitType,
                    HpRatio = (float)stats[i].CurValue / stats[i].MaxValue,
                    Entity = entities[i],
                });
            }
            UpdateSelectedUnitView(in _unitInfos);
        }

        #region PrivateMethods
        private void InitMultiUnitShowSlots( )
        {
            var panelRect = multiUnitPanel.GetComponent<RectTransform>();
            var cellWidth = (panelRect.rect.width - (cols - 1) * columnSpacing) / cols;
            var cellHeight = (panelRect.rect.height - (rows - 1) * rowSpacing) / rows;

            for (var r = 0; r < rows; r++)
            {
                for (var c = 0; c < cols; c++)
                {
                    var slot = Instantiate(slotPrefab, multiUnitPanel.transform);
                    var slotRect = slot.GetComponent<RectTransform>();
                    var posX = startPos.x + c * (cellWidth + columnSpacing);
                    var posY = startPos.y - r * (cellHeight + rowSpacing);
                    slotRect.sizeDelta = new Vector2(cellWidth, cellHeight);
                    slotRect.anchoredPosition = new Vector2(posX, posY);
                    slot.SetActive(false);
                    var slotMono = slot.GetComponent<MultiUnitShowSlot>();
                    slotMono.index = r * cols + c;
                    slotMono.button.onClick.AddListener((() => { OnClickSlotImage(slotMono.index); }));
                    _slots.Add(slot);
                }
            }
            multiUnitPanel.SetActive(false);
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
                    var unitShowSlot = _slots[i].GetComponent<MultiUnitShowSlot>();
                    var unitInfo = selectedUnitInfo[startIdx + i];
                    unitShowSlot.image.sprite = _unitSpriteDict[unitInfo.UnitType];
                    unitShowSlot.slider.value = unitInfo.HpRatio;
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

        private void ClearGrid()
        {
            foreach (Transform child in multiUnitPanel.transform)
            {
                Destroy(child.gameObject);
            }
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
        #endregion


        #region DataStructure


        
        [Serializable]
        public struct UnitTypeSpritePair
        {
            public UnitType unitType;
            public Sprite sprite;
        }

        private struct UnitInfo
        {
            public UnitType UnitType;
            public float HpRatio;
            public Entity Entity;
        }
        #endregion

    }
}