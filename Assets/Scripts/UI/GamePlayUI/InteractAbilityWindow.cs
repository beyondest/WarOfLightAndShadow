using System;
using System.Collections.Generic;
using System.Globalization;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Interact;
using Unity.Entities;
using UnityEngine;
using SparFlame.UI.General;
using Unity.Mathematics;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace SparFlame.UI.GamePlay
{
    public class InteractAbilityWindow : UIUtils.SingleTargetWindow
    {
        public static InteractAbilityWindow Instance;

        [Header("Custom Config")] public GameObject interactAbilityPanel;
        public Button attackBar;
        public Button healBar;
        public Button harvestBar;
        
        [Tooltip("Name must be : AttackAmount = Attack + Amount," +
                 "prefix can be Attack, Heal, Harvest" +
                 "suffix can be Speed, Amount, Range, Targets." +
                 "Notice that range refers to rangeSq, not the range itself when calculating, " +
                 "but for player, we show rangeSq as if it is range")]
        public List<AbilityNameSpritePair> abilitySprites;

        public AssetReferenceGameObject attrSlotRef;
        public UIUtils.MultiShowSlotConfig config;

        public override void Show(Vector2? pos = null)
        {
            interactAbilityPanel.SetActive(true);
        }

        public override void Hide()
        {
            interactAbilityPanel.SetActive(false);
            _targetEntity = Entity.Null;
        }

        public override bool TrySwitchTarget(Entity target)
        {
            var attackable = _em.HasComponent<AttackAbility>(target);
            var healable = _em.HasComponent<HealAbility>(target);
            var harvestable = _em.HasComponent<HarvestAbility>(target);

            if (!attackable && !healable && !harvestable) return false;
            
            attackBar.interactable  = attackable;
            healBar.interactable  = healable;
            harvestBar.interactable  = harvestable;
            _targetEntity = target;
            // Use this sequence to ensure attack ability show first if it has
            if (harvestable) OnClickHarvestBar();
            if (healable) OnClickHealBar();
            if (attackable) OnClickAttackBar();
            return true;

        }

        public override bool HasTarget()
        {
            return _targetEntity != Entity.Null;
        }


        public override bool IsOpened()
        {
            return interactAbilityPanel.activeSelf;
        }

        #region ButtonMethods

        public void OnClickAttackBar()
        {
            var ability = _em.GetComponentData<AttackAbility>(_targetEntity);
            UpdateInteractAbilityInfo(ability);
            _currentBar = InteractType.Attack;
        }

        public void OnClickHealBar()
        {
            var ability = _em.GetComponentData<HealAbility>(_targetEntity);
            UpdateInteractAbilityInfo(ability);
            _currentBar = InteractType.Heal;
        }

        public void OnClickHarvestBar()
        {
            var ability = _em.GetComponentData<HarvestAbility>(_targetEntity);
            UpdateInteractAbilityInfo(ability);
            _currentBar = InteractType.Harvest;
        }

        #endregion

        private GameObject _attrSlotPrefab;
        private readonly List<GameObject> _slots = new();
        private readonly Dictionary<string, Sprite> _sprites = new();
        private Dictionary<InteractType, List<Sprite>> _spritesByInteractType = new();
        private InteractType _currentBar;
        private AsyncOperationHandle<GameObject> _slotHandle;

        private Entity _targetEntity;
        private EntityManager _em;
        private EntityQuery _notPauseTag;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private void OnEnable()
        {
            _slotHandle = CR.LoadAssetRefAsync<GameObject>(attrSlotRef, go =>
            {
                _attrSlotPrefab = go;
                UIUtils.InitMultiShowSlotsByIndex(_slots, interactAbilityPanel, _attrSlotPrefab, in config);
            });
        }

        private void Start()
        {
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            _notPauseTag = _em.CreateEntityQuery(typeof(NotPauseTag));
            foreach (var pair in abilitySprites)
            {
                _sprites.Add(pair.name, pair.sprite);
            }
            Hide();
             
        }
        
        private void Update()
        {
            if (_notPauseTag.IsEmpty) return;
            if (!IsOpened()) return;
            if (_targetEntity == Entity.Null) return;

            switch (_currentBar)
            {
                case InteractType.Attack:
                    UpdateInteractAbilityInfo(_em.GetComponentData<AttackAbility>(_targetEntity));
                    break;
                case InteractType.Heal:
                    UpdateInteractAbilityInfo(_em.GetComponentData<HealAbility>(_targetEntity));
                    break;
                case InteractType.Harvest:
                    UpdateInteractAbilityInfo(_em.GetComponentData<HarvestAbility>(_targetEntity));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void UpdateInteractAbilityInfo(IInteractAbility interactAbility)
        {
            var properties = typeof(IInteractAbility).GetProperties();
            var spriteList =
                UnitWindowResourceManager.Instance.InteractAbilitySprites[interactAbility.InteractType];
            var prefix = interactAbility.InteractType switch
            {
                InteractType.Attack => "Attack",
                InteractType.Heal => "Heal",
                InteractType.Harvest => "Harvest",
                _ => throw new ArgumentOutOfRangeException()
            };
            
            for (var i = 0; i < _slots.Count; i++)
            {
                if (i <spriteList.Count) // Length - 1 is because last property is InteractType
                {
                    var oriName = properties[i].Name;
                 
                    _slots[i].SetActive(true);
                    var attrSlot = _slots[i].GetComponent<AttributeSlot>();
                    var value = properties[i].GetValue(interactAbility);
                    if (oriName == "Range" && value is float floatValue)
                    {
                        value = math.sqrt(floatValue);
                    }
                    var newName = prefix + oriName;
                    attrSlot.icon.sprite = spriteList[i];
                    attrSlot.label.text = newName + ":";
                    attrSlot.value.text = value.ToString();
                }
                else
                {
                    _slots[i].SetActive(false);
                }
            }
        }
    }

    [Serializable]
    public struct AbilityNameSpritePair
    {
        public string name;
        public Sprite sprite;
    }
}