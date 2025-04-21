using System;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Interact;
using SparFlame.UI.General;
using TMPro;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace SparFlame.UI.GamePlay
{
    public class ConjureSlot : MultiShowSlot
    {
        [Header("General")] [SerializeField] private Slider conjureCountSlider;
        [SerializeField] private TMP_Text conjureCountText;
        [SerializeField] private Image tierIcon;
        [SerializeField] private TMP_Text hpText;
        [SerializeField] private Image hpImage;
        [SerializeField] private TMP_Text gameplayNameText;


        [Header("VFX")] [SerializeField] private Color cannotColor = Color.gray;
        [SerializeField] private Image cannotColorChangeImage;

        [Header("Sub slots")] [SerializeField] private ConjureDetailInfoSlot detailInfoSlot;
        [SerializeField] private InteractAbilitySlot interactAbilitySlot;


        // TODO : Change All multi slots to Set target, input is spriteEntityInfo
        public void SetTarget(in SpriteEntityInfo info)
        {
            //  Update basic ui info
            _targetEntity = info.EntityPrefab;
            gameplayNameText.text = info.GameplayName;
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            tierIcon.sprite = BasicResourceManager.Instance.TierSprites[info.Tier];
            _faction = _em.GetComponentData<GeneralAttr>(info.EntityPrefab).FactionTag;
            tierIcon.color = _faction == FactionTag.Ally ? Color.white : Color.black;
            hpText.text = _em.GetComponentData<StatData>(_targetEntity).MaxValue.ToString();
            hpImage.sprite = BasicResourceManager.Instance.FactionHpSprites[_faction];

            // Update detail panel and interact ability panel
            detailInfoSlot.TrySwitchTarget(info.EntityPrefab);
            interactAbilitySlot.TrySwitchTarget(info.EntityPrefab);
            UpdateDynamicData();
        }

        public int GetConjureCount() => (int)conjureCountSlider.value;
        public int GetMaxConjureCount() => (int)conjureCountSlider.maxValue;

        // Internal Data
        private EntityManager _em;
        private EntityQuery _notPauseTag;
        private Entity _targetEntity = Entity.Null;
        private FactionTag _faction;
        private bool _preEnabled;

        private void OnEnable()
        {
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            _notPauseTag = _em.CreateEntityQuery(typeof(NotPauseTag));
            conjureCountSlider.onValueChanged.AddListener(arg0 =>
            {
                detailInfoSlot.SetCurConjureCount((int)conjureCountSlider.value);
                detailInfoSlot.UpdateCostSlots();
            });
        }

        private void OnDisable()
        {
            conjureCountSlider.onValueChanged.RemoveAllListeners();
            _preEnabled = false;
        }


        private void Update()
        {
            if (_notPauseTag.IsEmpty) return;
            if (_targetEntity == Entity.Null) return;

            UpdateDynamicData();
        }

        private void UpdateDynamicData()
        {
            var maxCount = GameplayUIUtils.CalMaxCountForConjureOrConstruct(_faction,
                _em, _targetEntity);

            // Cannot conjure this unit even for one
            if (maxCount == 0)
            {
                if (_preEnabled)
                {
                    _preEnabled = false;
                    UIMathMethods.AnimateColorAsync(cannotColorChangeImage, cannotColorChangeImage.color,
                        cannotColor, 1f);
                    button!.enabled = false;
                }

                conjureCountSlider.maxValue = conjureCountSlider.value = 0;
            }
            else
            {
                if (!_preEnabled)
                {
                    UIMathMethods.AnimateColorAsync(cannotColorChangeImage, cannotColorChangeImage.color,
                        Color.white, 1f);
                    button!.enabled = true;
                    _preEnabled = true;
                }
                // Can conjure some unit
                conjureCountSlider.maxValue = maxCount;
                // Set minimum conjure count to 1
                if (conjureCountSlider.value == 0) conjureCountSlider.value = 1;
            }
            conjureCountText.text = $"{(int)conjureCountSlider.value}/{maxCount}";
        }
    }
}