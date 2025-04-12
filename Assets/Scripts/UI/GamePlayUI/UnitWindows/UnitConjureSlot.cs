using System;
using System.Globalization;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Interact;
using SparFlame.GamePlaySystem.Movement;
using SparFlame.GamePlaySystem.Resource;
using SparFlame.GamePlaySystem.Units;
using SparFlame.GamePlaySystem.UnitSelection;
using SparFlame.UI.General;
using TMPro;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace SparFlame.UI.GamePlay
{
    public class UnitConjureSlot : MultiShowSlot
    {
        public Image unitIcon;
        public Slider conjureCountSlider; 

        [SerializeField] private TMP_Text gamePlayNameText;
        [SerializeField] private Color cannotConjureColor = Color.gray;
        // For set gray color if resource is not able to support conjure it
        [SerializeField] private Image slotPanelImage;

        [SerializeField]
        private UnitConjureDetailInfoSlot detailInfoSlot;
        [SerializeField]
        private UnitConjureInteractAbilitySlot interactAbilitySlot;

        public override void SetTarget(Entity target)
        {
            _targetEntity = target;
            var interactAttr = _em.GetComponentData<InteractableAttr>(target);
            gamePlayNameText.text = interactAttr.GameplayName.ToString();
            detailInfoSlot.TrySwitchTarget(target);
            interactAbilitySlot.TrySwitchTarget(target);
            CalculateMaxConjureCountAndSetSlider();
            UIMathMethods.AnimateColorAsync(slotPanelImage, slotPanelImage.color,
                conjureCountSlider.maxValue == 0 ? cannotConjureColor : Color.white, 1f);
        }

        private EntityQuery _selectionData;
        private EntityManager _em;
        private EntityQuery _notPauseTag;
        private Entity _targetEntity = Entity.Null;

        private void OnEnable()
        {
            _selectionData = _em.CreateEntityQuery(typeof(UnitSelectionData));
            _notPauseTag = _em.CreateEntityQuery(typeof(NotPauseTag));
        }

        private void Update()
        {
            if (_notPauseTag.IsEmpty) return;
            if (_targetEntity == Entity.Null) return;
            CalculateMaxConjureCountAndSetSlider();
        }

        private void CalculateMaxConjureCountAndSetSlider()
        {
            var selectionData = _selectionData.GetSingleton<UnitSelectionData>();
            EntityQuery query;
            switch (selectionData.CurrentSelectFaction)
            {
                case FactionTag.Ally:
                    query = _em.CreateEntityQuery(typeof(AllyResourceDataTag));
                    break;
                case FactionTag.Enemy:
                    query = _em.CreateEntityQuery(typeof(EnemyResourceDataTag));
                    break;
                case FactionTag.Neutral:
                    return;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var resourceEntity = query.GetSingletonEntity();
            var resourceData = _em.GetBuffer<ResourceData>(resourceEntity);
            var costData = _em.GetBuffer<CostList>(_targetEntity);
            var maxCount = 0;
            foreach (var cost in costData)
            {
                var count = resourceData[(int)cost.Type].Amount / cost.Amount;
                if (count > maxCount)
                    maxCount = count;
            }

            // Cannot conjure even single unit
            if (maxCount == 0)
            {
                conjureCountSlider.maxValue = conjureCountSlider.value = 0;
                return;
            }

            conjureCountSlider.maxValue = maxCount;
            if (conjureCountSlider.value == 0) conjureCountSlider.value = 1;
        }
    }

    public class UnitConjureDetailInfoSlot : UnitDetailWindow
    {
        protected override void Awake()
        {
        }

        protected override void Start()
        {
        }

        protected override void Update()
        {
        }
    }


    public class UnitConjureInteractAbilitySlot : InteractAbilityWindow
    {
        protected override void Awake()
        {
        }

        protected override void Start()
        {
        }

        protected override void Update()
        {
        }
    }
}