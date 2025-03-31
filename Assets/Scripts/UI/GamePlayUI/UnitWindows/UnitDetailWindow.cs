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
    public class UnitDetailWindow : SingleTargetWindow
    {
        public static UnitDetailWindow Instance;

        [Header("Custom Config")] public GameObject unitDetailPanel;
        public TMP_Text unitType;
        public TMP_Text unitHp;
        public TMP_Text unitMoveSpeed;
        public Image unitIcon;
        public Image hpIcon;
        [CanBeNull] public string unitType2DSpriteSuffix;
        [CanBeNull] public string resourceTypeSpriteSuffix;
        [CanBeNull] public string factionTypeHpSpriteSuffix;

        [Header("Multi show config")] public AssetReferenceGameObject costSlotPrefab;
        public MultiShowSlotConfig config;

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
        private List<GameObject> _costSlots = new();
        private readonly Dictionary<ResourceType, Sprite> _resourceSprites = new();
        private readonly Dictionary<UnitType, Sprite> _unitSprites = new();
        private readonly Dictionary<FactionTag, Sprite> _factionHpSprites = new();
        private AsyncOperationHandle<IList<Sprite>> _unitSpritesHandle;
        private AsyncOperationHandle<IList<Sprite>> _resourceSpritesHandle;
        private AsyncOperationHandle<IList<Sprite>> _factionHpSpritesHandle;
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
            _unitSpritesHandle = CR.LoadTypeSuffixAddressableAsync<UnitType, Sprite>(unitType2DSpriteSuffix,
                result => CR.OnTypeSuffixAddressableLoadComplete(result, _unitSprites));
            _resourceSpritesHandle = CR.LoadTypeSuffixAddressableAsync<ResourceType, Sprite>(resourceTypeSpriteSuffix,
                result => CR.OnTypeSuffixAddressableLoadComplete(result, _resourceSprites));
            _factionHpSpritesHandle = CR.LoadTypeSuffixAddressableAsync<FactionTag, Sprite>(factionTypeHpSpriteSuffix,
                result => CR.OnTypeSuffixAddressableLoadComplete(result, _factionHpSprites));
            _costSlotPrefabHandle =
                CR.LoadPrefabAddressableRefAsync<GameObject>(costSlotPrefab, result =>
                {
                    _costSlotPrefab = result;
                    UIUtils.InitMultiShowSlots(ref _costSlots, unitDetailPanel, _costSlotPrefab, in config);
                });
        }

        private void OnDisable()
        {
            Addressables.Release(_resourceSpritesHandle);
            Addressables.Release(_unitSpritesHandle);
            Addressables.Release(_factionHpSpritesHandle);
            Addressables.Release(_costSlotPrefabHandle);
            _unitSprites.Clear();
            _resourceSprites.Clear();
            _factionHpSprites.Clear();
            _costSlots.Clear();
        }

        private void Start()
        {
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            _notPauseTag = _em.CreateEntityQuery(typeof(NotPauseTag));

            unitDetailPanel.SetActive(false);
        }

        private void Update()
        {
            if (_notPauseTag.IsEmpty) return;
            if (!IsOpened()) return;
            if (!_unitSpritesHandle.IsDone || !_resourceSpritesHandle.IsDone || !_factionHpSpritesHandle.IsDone) return;
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