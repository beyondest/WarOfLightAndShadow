using System;
using System.Collections.Generic;
using SparFlame.GamePlaySystem.General;
using Unity.Entities;
using UnityEngine;
using SparFlame.UI.General;
using TMPro;
using Unity.Mathematics;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace SparFlame.UI.GamePlay
{
    public class InteractAbilityWindow : MonoBehaviour, MultiSlotWindowUtils.ISingleTargetWindow
    {
        [Header("Custom Config")] [SerializeField]
        private float colorSwitchDuration = 1.0f;
        [Tooltip("This list must in sequence of interact type : Attack, Heal, Harvest ")]
        [SerializeField] private List<InteractTypeColorPair> interactTypeColorPairs;

        [Header("Internal Config")] [SerializeField]
        private GameObject panel;

        [SerializeField] private GameObject attackBar;
        [SerializeField] private GameObject healBar;
        [SerializeField] private GameObject harvestBar;

        [SerializeField] private TMP_Text amountLabelText;
        [SerializeField] private TMP_Text amountValueText;
        [SerializeField] private Image amountIcon;
        [SerializeField] private TMP_Text rangeLabelText;
        [SerializeField] private TMP_Text rangeValueText;
        [SerializeField] private Image rangeIcon;
        [SerializeField] private TMP_Text speedLabelText;
        [SerializeField] private TMP_Text speedValueText;
        [SerializeField] private Image speedIcon;
        [SerializeField] private TMP_Text targetsLabelText;
        [SerializeField] private TMP_Text targetsValueText;
        [FormerlySerializedAs("targetIcon")] [SerializeField] private Image targetsIcon;

        // Interface
        public static InteractAbilityWindow Instance;

        public void Show(Vector2? pos = null)
        {
            panel.SetActive(true);
        }

        public void Hide()
        {
            panel.SetActive(false);
            _targetEntity = Entity.Null;
        }

        public bool IsOpened()
        {
            return panel.activeSelf;
        }

        public bool TrySwitchTarget(Entity target)
        {
            Em = World.DefaultGameObjectInjectionWorld.EntityManager;
            var attackable = Em.HasComponent<AttackAbility>(target);
            var healable = Em.HasComponent<HealAbility>(target);
            var harvestable = Em.HasComponent<HarvestAbility>(target);

            if (!attackable && !healable && !harvestable) return false;

            attackBar.SetActive(attackable);
            healBar.SetActive(healable);
            harvestBar.SetActive(harvestable);
            _targetEntity = target;

            // Use this sequence to ensure attack ability show first if it has
            if (harvestable) OnClickHarvestBar();
            if (healable) OnClickHealBar();
            if (attackable) OnClickAttackBar();

            return true;
        }

        public bool HasTarget()
        {
            return _targetEntity != Entity.Null;
        }

        #region ButtonMethods

        public void OnClickAttackBar()
        {
            var ability = Em.GetComponentData<AttackAbility>(_targetEntity);
            UpdateInteractAbilityInfo(ability);
            _currentBar = InteractType.Attack;
            ChangeColorGradually();
        }

        public void OnClickHealBar()
        {
            var ability = Em.GetComponentData<HealAbility>(_targetEntity);
            UpdateInteractAbilityInfo(ability);
            _currentBar = InteractType.Heal;
            ChangeColorGradually();
        }

        public void OnClickHarvestBar()
        {
            var ability = Em.GetComponentData<HarvestAbility>(_targetEntity);
            UpdateInteractAbilityInfo(ability);
            _currentBar = InteractType.Harvest;
            ChangeColorGradually();
        }

        #endregion

        private GameObject _attrSlotPrefab;
        private InteractType _currentBar;
        private AsyncOperationHandle<GameObject> _slotHandle;

        private Entity _targetEntity;
        protected EntityManager Em;
        private EntityQuery _gamingTag;

        protected virtual void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }
        
        protected virtual void Start()
        {
            Em = World.DefaultGameObjectInjectionWorld.EntityManager;
            _gamingTag = Em.CreateEntityQuery(typeof(GamingTag));
            for (var i = 0; i < interactTypeColorPairs.Count; i++)
            {
                var pair = interactTypeColorPairs[i];
                if ((int)pair.type != i)
                    throw new ArgumentException(
                        "InteractAbility Window parameters wrong, color pairs must match the sequence of " +
                        "Interact type enum");
            }
            Hide();
        }

        protected virtual void Update()
        {
            if (_gamingTag.IsEmpty) return;
            if (!IsOpened()) return;
            if (_targetEntity == Entity.Null) return;
            if (!Em.HasComponent<GeneralAttr>(_targetEntity))
            {
                _targetEntity = Entity.Null;
                return;
            }

            switch (_currentBar)
            {
                case InteractType.Attack:
                    UpdateInteractAbilityInfo(Em.GetComponentData<AttackAbility>(_targetEntity));
                    break;
                case InteractType.Heal:
                    UpdateInteractAbilityInfo(Em.GetComponentData<HealAbility>(_targetEntity));
                    break;
                case InteractType.Harvest:
                    UpdateInteractAbilityInfo(Em.GetComponentData<HarvestAbility>(_targetEntity));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void UpdateInteractAbilityInfo(IInteractAbility interactAbility)
        {
            var prefix = interactAbility.InteractType switch
            {
                InteractType.Attack => "Attack",
                InteractType.Heal => "Heal",
                InteractType.Harvest => "Harvest",
                _ => throw new ArgumentOutOfRangeException()
            };
            amountLabelText.text = prefix + " AbsAmount";
            amountValueText.text = interactAbility.Amount.ToString();
            rangeLabelText.text = prefix + " Range";
            rangeValueText.text = math.sqrt(interactAbility.RangeSq).ToString("F2");
            speedLabelText.text = prefix + " Speed";
            speedValueText.text = interactAbility.Speed.ToString("F2");
            targetsLabelText.text = prefix + " Targets";
            targetsValueText.text = ((int)interactAbility.Targets).ToString();
        }

        private void ChangeColorGradually()
        {
            UIMathMethods.AnimateColorAsync(amountIcon, amountIcon.color,
                interactTypeColorPairs[(int)_currentBar].color,
                colorSwitchDuration);
            UIMathMethods.AnimateColorAsync(speedIcon, speedIcon.color, interactTypeColorPairs[(int)_currentBar].color,
                colorSwitchDuration);
            UIMathMethods.AnimateColorAsync(rangeIcon, rangeIcon.color, interactTypeColorPairs[(int)_currentBar].color,
                colorSwitchDuration);
            UIMathMethods.AnimateColorAsync(targetsIcon, targetsIcon.color, interactTypeColorPairs[(int)_currentBar].color,
                colorSwitchDuration);
        }

        [Serializable]
        public struct InteractTypeColorPair
        {
            public InteractType type;
            public Color color;
        }
    }
}