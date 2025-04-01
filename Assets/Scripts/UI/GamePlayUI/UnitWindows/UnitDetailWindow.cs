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
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace SparFlame.UI.GamePlay
{
    public class UnitDetailWindow : UIUtils.SingleTargetWindow
    {
        // Config
        [Header("Custom Config")] public GameObject unitDetailPanel;
        public TMP_Text unitType;
        public TMP_Text unitHp;
        public TMP_Text unitMoveSpeed;
        public Image unitIcon;
        public Image hpIcon;

        [Header("Multi show config")] public AssetReferenceGameObject costSlotPrefab;
        public UIUtils.MultiShowSlotConfig config;

        // Interface
        public static UnitDetailWindow Instance;
        public override void Show(Vector2? pos = null)
        {
            unitDetailPanel.SetActive(true);
        }

        public override void Hide()
        {
            unitDetailPanel.SetActive(false);

            _targetEntity = Entity.Null;
        }

        public override bool TrySwitchTarget(Entity target)
        {
            if (!_em.HasComponent<UnitAttr>(target)
                || !_em.HasComponent<MovableData>(target)
                || !_em.HasBuffer<CostList>(target))
                return false;
            _targetEntity = target;
            return true;
        }

        public override bool HasTarget()
        {
            return _targetEntity != Entity.Null;
        }

        public override bool IsOpened()
        {
            return unitDetailPanel.activeSelf;
        }

        private GameObject _costSlotPrefab;
        private Entity _targetEntity = Entity.Null;
        private readonly List<GameObject> _costSlots = new();

        private AsyncOperationHandle<GameObject> _costSlotPrefabHandle;
        private EntityManager _em;
        private EntityQuery _notPauseTag;

        #region EventFunction

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnEnable()
        {
            
            _costSlotPrefabHandle =
                CR.LoadAssetRefAsync<GameObject>(costSlotPrefab, result =>
                {
                    _costSlotPrefab = result;
                    UIUtils.InitMultiShowSlotsByIndex(_costSlots, unitDetailPanel, _costSlotPrefab, in config);
                });
        }

        private void OnDisable()
        {

            Addressables.Release(_costSlotPrefabHandle);
            _costSlots.Clear();
            unitDetailPanel.SetActive(false);
        }

        private void Start()
        {
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            _notPauseTag = _em.CreateEntityQuery(typeof(NotPauseTag));
        }

        private void Update()
        {
            if (_notPauseTag.IsEmpty) return;
            if (!IsOpened()) return;
            
            if (!UnitWindowResourceManager.Instance.IsResourceLoaded()) return;
            if (_targetEntity != Entity.Null)
                UpdateUnitDetailInfo();
        }

        #endregion


        private void UpdateUnitDetailInfo()
        {
            // First time need to update those static attributes
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
            unitIcon.sprite = UnitWindowResourceManager.Instance.UnitSprites[attr.Type];
            hpIcon.sprite = UnitWindowResourceManager.Instance.FactionHpSprites[interactableAttr.FactionTag];
            for (int i = 0; i < _costSlots.Count; i++)
            {
                if (i < costList.Length)
                {
                    _costSlots[i].SetActive(true);
                    var cost = costList[i];
                    var costSlot = _costSlots[i].GetComponent<AttributeSlot>();
                    costSlot.icon.sprite = UnitWindowResourceManager.Instance.ResourceSprites[cost.Type];
                    costSlot.label.text = cost.Type.ToString();
                    costSlot.value.text = cost.Amount.ToString();
                }
                else
                {
                    _costSlots[i].SetActive(false);
                }
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