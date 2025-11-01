using System;
using System.Collections.Generic;
using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using SparFlame.Core.Utils;
using Unity.Entities;
using UnityEngine;
using SparFlame.UI.General;
using TMPro;
using UnityEngine.UI;

namespace SparFlame.UI.SubGameplay
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
        [SerializeField] private TMP_Text amountBonusText;
        [SerializeField] private Image amountIcon;
        [SerializeField] private TMP_Text rangeLabelText;
        [SerializeField] private TMP_Text rangeValueText;
        [SerializeField] private TMP_Text rangeBonusText;
        [SerializeField] private Image rangeIcon;
        [SerializeField] private TMP_Text speedLabelText;
        [SerializeField] private TMP_Text speedValueText;
        [SerializeField] private TMP_Text speedBonusText;
        [SerializeField] private Image speedIcon;
        [SerializeField] private TMP_Text targetsLabelText;
        [SerializeField] private TMP_Text targetsValueText;
        [SerializeField] private TMP_Text targetsBonusText;
        [SerializeField] private Image targetsIcon;

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
        public void ClearCloseUpTarget()
        {
            _targetEntity = Entity.Null;
            
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
        private Vector2 _originalPos;

        private Entity _targetEntity;
        protected EntityManager Em;
        private EntityQuery _gamingTag;

        protected virtual void Awake()
        {
            if (!Instance)
                Instance = this;
            else
                Destroy(gameObject);
        }
        
        protected virtual void Start()
        {
            Em = World.DefaultGameObjectInjectionWorld.EntityManager;
            if(_gamingTag != default)_gamingTag.Dispose();
            _gamingTag = Em.CreateEntityQuery(typeof(SubGamingTag));
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
            if (!Em.HasComponent<SubGameplayGeneralAttr>(_targetEntity))
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
                    BurstSafe.UnexpectedEnum(_currentBar);
                    break;
            }
        }

        private void OnDestroy()
        {
            try
            {
                if(_gamingTag != default)_gamingTag.Dispose();
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void UpdateInteractAbilityInfo(IInteractAbility interactAbility)
        {
            var bonus = Em.GetComponentData<InteractAbilityBonus>(_targetEntity);
            var prefix = interactAbility.InteractType switch
            {
                InteractType.Attack => "Attack",
                InteractType.Heal => "Heal",
                InteractType.Harvest => "Harvest",
                _ => BurstSafe.UnexpectedEnum(interactAbility.InteractType, "Wrong")
            };
            
            amountLabelText.text = prefix + " AbsAmount";
            amountValueText.text = (interactAbility.Amount + bonus.AmountBonus).ToString() ;
            if (bonus.AmountBonus == 0)
            {
                amountBonusText.enabled = false;
            }
            else
            {
                amountBonusText.enabled = true;
                var signal = bonus.AmountBonus > 0 ? "+" : "-";
                amountBonusText.text = $"({signal}{bonus.AmountBonus})";
                amountBonusText.color = bonus.AmountBonus > 0 ? Color.green : Color.red;
            }
            
            rangeLabelText.text = prefix + " Range";
            rangeValueText.text = (interactAbility.Range + bonus.RangeBonus).ToString("F1") ;
            if (bonus.RangeBonus == 0)
            {
                rangeBonusText.enabled = false;
            }
            else
            {
                rangeBonusText.enabled = true;
                var signal = bonus.RangeBonus > 0 ? "+" : "-";
                rangeBonusText.text =$"({signal}{bonus.RangeBonus:F1})";
                rangeBonusText.color = bonus.RangeBonus > 0 ? Color.green : Color.red;
            }
            
            speedLabelText.text = prefix + " Speed";
            speedValueText.text = (interactAbility.Speed + bonus.SpeedBonus).ToString("F1");
            if (bonus.SpeedBonus == 0)
            {
                speedBonusText.enabled = false;
            }
            else
            {
                speedBonusText.enabled = true;
                var signal = bonus.RangeBonus > 0 ? "+" : "-";
                speedBonusText.text =$"({signal}{bonus.SpeedBonus:F1})";
                speedBonusText.color = bonus.SpeedBonus > 0 ? Color.green : Color.red;
            }
            
            targetsLabelText.text = prefix + " Targets";
            targetsValueText.text = (interactAbility.Targets + bonus.TargetsBonus).ToString() ;
            if (bonus.TargetsBonus == 0)
            {
                targetsBonusText.enabled = false;
            }
            else
            {
                targetsBonusText.enabled = true;
                var signal = bonus.TargetsBonus > 0 ? "+" : "-";
                targetsBonusText.text = $"({signal}{bonus.TargetsBonus})";
                targetsBonusText.color = bonus.TargetsBonus > 0 ? Color.green : Color.red;
            }
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