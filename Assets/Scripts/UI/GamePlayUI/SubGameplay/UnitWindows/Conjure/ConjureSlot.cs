using System;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Interact;
using SparFlame.UI.General;
using SparFlame.Utils;
using TMPro;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace SparFlame.UI.SubGameplay
{
    public class ConjureSlot : MultiShowSlot, CustomDs.IResourceManager
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


        public void SetTarget(in SpriteEntityInfo info)
        {
            //  Update basic ui info
            _targetEntity = info.EntityPrefab;
            gameplayNameText.text = info.GameplayName;
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            tierIcon.sprite = BasicUIResourceManager.Instance.TierSprites[info.Tier];
            _faction = _em.GetComponentData<SubGameplayGeneralAttr>(info.EntityPrefab).FactionTag;
            tierIcon.color = _faction == FactionTag.Ally ? Color.white : Color.black;
            hpText.text = _em.GetComponentData<StatData>(_targetEntity).maxValue.ToString();
            hpImage.sprite = BasicUIResourceManager.Instance.FactionHpSprites[_faction];

            // Update detail panel and interact ability panel
            detailInfoSlot.TrySwitchTarget(info.EntityPrefab);
            interactAbilitySlot.TrySwitchTarget(info.EntityPrefab);
            UpdateDynamicData();
        }

        public int GetConjureCount() => (int)conjureCountSlider.value;
        public int GetMaxConjureCount() => (int)conjureCountSlider.maxValue;

        // Internal Data
        private EntityManager _em;
        private EntityQuery _gamingTag;
        private Entity _targetEntity = Entity.Null;
        private FactionTag _faction;
        private bool _preEnabled;



        private void OnDisable()
        {
            _preEnabled = false;
        }


        private void Update()
        {
            if (_gamingTag.IsEmpty) return;
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

        public bool IsInitialized => detailInfoSlot.IsInitialized;
        public float InitProgress => detailInfoSlot.InitProgress;
        public void LoadResources()
        {
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            _gamingTag = _em.CreateEntityQuery(typeof(SubGamingTag));
            conjureCountSlider.onValueChanged.AddListener(_ =>
            {
                detailInfoSlot.SetCurConjureCount((int)conjureCountSlider.value);
                detailInfoSlot.UpdateCostSlots();
            });
            detailInfoSlot.LoadResources();
        }

        public void UnloadResources()
        {
            conjureCountSlider.onValueChanged.RemoveAllListeners();
            detailInfoSlot.UnloadResources();
        }
    }
}