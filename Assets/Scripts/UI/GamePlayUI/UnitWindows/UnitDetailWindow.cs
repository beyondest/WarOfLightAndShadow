using System;
using System.Collections.Generic;
using System.Globalization;
using JetBrains.Annotations;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Interact;
using SparFlame.GamePlaySystem.Movement;
using SparFlame.GamePlaySystem.Resource;
using SparFlame.GamePlaySystem.Units;
using SparFlame.UI.General;
using TMPro;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace SparFlame.UI.GamePlay
{
    public class UnitDetailWindow : SingleTargetWindow
    {
        public static UnitDetailWindow Instance;

        [Header("Custom Config")] public GameObject unitDetailPanel;
        public TMP_Text unitType;
        public TMP_Text unitHp;
        public TMP_Text unitMoveSpeed;
        public Image unitIcon;
        public Image hpIcon;
        public string unit2DSpritesPath;
        [CanBeNull] public string unit2DSpritePrefix;
        public string resourceTypeSpritesPath;
        [CanBeNull] public string resourceTypeSpritePrefix;
        public List<FactionTypeSpritePair> factionTypeHpSprite;
        
        [Header("Multi show config")] public GameObject costSlotPrefab;
        public MultiShowSlotConfig config;


        public override void Show(Vector2? pos = null)
        {
            unitDetailPanel.SetActive(true);
            UpdateUnitDetailInfo(true);
        }
        
        public override void Hide()
        {
            unitDetailPanel.SetActive(false);
            _targetEntity = Entity.Null;
        }
        
        public override bool TrySwitchTarget(Entity target)
        {
            if(!_em.HasComponent<UnitAttr>(target)
               || !_em.HasComponent<MovableData>(target)
               || !_em.HasBuffer<CostList>(target))
                return false;
            _targetEntity = target;
            UpdateUnitDetailInfo(true);
            return true;
        }

        public override bool IsOpened()
        {
            return unitDetailPanel.activeSelf;
        }

        private Entity _targetEntity = Entity.Null;
        private List<GameObject> _costSlots = new();
        private Dictionary<ResourceType, Sprite> _resourceSprites;
        private Dictionary<UnitType, Sprite> _unitSprites;
        private readonly Dictionary<FactionTag, Sprite> _factionHpSprites = new();
        private EntityManager _em;
        private EntityQuery _notPauseTag;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            _notPauseTag = _em.CreateEntityQuery(typeof(NotPauseTag));
            UIUtils.InitMultiShowSlots(ref _costSlots, unitDetailPanel, costSlotPrefab, in config);
            _resourceSprites = UIUtils.LoadTypeSprites<ResourceType>(resourceTypeSpritesPath, resourceTypeSpritePrefix);
            _unitSprites = UIUtils.LoadTypeSprites<UnitType>(unit2DSpritesPath, unit2DSpritePrefix);
            foreach (var pair in factionTypeHpSprite)
            {
                _factionHpSprites.Add(pair.faction, pair.sprite);
            }
            Hide();
        }

        private void Update()
        {
            if (_notPauseTag.IsEmpty) return;
            if(!IsOpened())return;
            if (_targetEntity != Entity.Null)
                UpdateUnitDetailInfo(false);
        }

        private void UpdateUnitDetailInfo(bool firstTime)
        {
            // First time need to update those static attributes
            if (firstTime)
            {
                var interactableAttr = _em.GetComponentData<InteractableAttr>(_targetEntity);
                var attr = _em.GetComponentData<UnitAttr>(_targetEntity);
                var movableData = _em.GetComponentData<MovableData>(_targetEntity);
                var statData = _em.GetComponentData<StatData>(_targetEntity);
                var costList = _em.GetBuffer<CostList>(_targetEntity);
                // Visualize these attributes
                unitType.text = attr.Type.ToString();
                unitMoveSpeed.text = movableData.MoveSpeed.ToString(CultureInfo.InvariantCulture);
                unitHp.text = statData.CurValue.ToString(CultureInfo.InvariantCulture) + "/" +
                              statData.MaxValue.ToString(CultureInfo.InvariantCulture);
                unitIcon.sprite = _unitSprites[attr.Type];
                hpIcon.sprite = _factionHpSprites[interactableAttr.FactionTag];
                for (int i = 0; i < _costSlots.Count; i++)
                {
                    if (i < costList.Length)
                    {
                        _costSlots[i].SetActive(true);
                        var cost = costList[i];
                        var costSlot = _costSlots[i].GetComponent<AttributeSlot>();
                        costSlot.icon.sprite = _resourceSprites[cost.Type];
                        costSlot.label.text = cost.Type.ToString();
                        costSlot.value.text = cost.Amount.ToString();
                    }
                    else
                    {
                        _costSlots[i].SetActive(false);
                    }
                }
            }
            // Not first time only need to update dynamic attributes
            else
            {
                var movableData = _em.GetComponentData<MovableData>(_targetEntity);
                var statData = _em.GetComponentData<StatData>(_targetEntity);
                unitMoveSpeed.text = movableData.MoveSpeed.ToString(CultureInfo.InvariantCulture);
                unitHp.text = statData.CurValue.ToString(CultureInfo.InvariantCulture) + "/" +
                              statData.MaxValue.ToString(CultureInfo.InvariantCulture);
            }
        }
    }

    [Serializable]
    public struct ResourceTypeSpritePair
    {
        public ResourceType type;
        public Sprite sprite;
    }
    [Serializable]
    public struct FactionTypeSpritePair
    {
        public FactionTag faction;
        public Sprite sprite;
    }
}