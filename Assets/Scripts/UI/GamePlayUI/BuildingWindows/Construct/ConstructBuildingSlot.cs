using System;
using SparFlame.Database;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Interact;
using SparFlame.UI.GamePlay.UI.GamePlayUI.BuildingWindows.Construct;
using SparFlame.UI.General;
using TMPro;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace SparFlame.UI.GamePlay
{
    public class ConstructBuildingSlot : MultiShowSlot
    {
        [Header("General Settings")] [SerializeField]
        private Image tierIcon;

        [SerializeField] private TMP_Text gameplayNameText;
        [SerializeField] private TMP_Text hpText;
        [SerializeField] private Image hpImage;

        [Header("VFX")] [SerializeField] private Color cannotColor = Color.gray;
        [SerializeField] private Image cannotColorChangeImage;

        [Header("Sub slots")] [SerializeField] private ConstructDetailInfoSlot detailInfoSlot;
        [SerializeField] private InteractAbilitySlot interactAbilitySlot;

        public void SetTarget(in SpriteEntityInfo info)
        {
            //  Update basic ui info
            _targetEntity = info.EntityPrefab;
            gameplayNameText.text = info.GameplayName;
            tierIcon.sprite = BasicResourceManager.Instance.TierSprites[info.Tier];
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            _faction = _em.GetComponentData<GeneralAttr>(info.EntityPrefab).FactionTag;
            tierIcon.color = _faction == FactionTag.Ally ? Color.white : Color.black;
            hpText.text = _em.GetComponentData<StatData>(_targetEntity).MaxValue.ToString();
            hpImage.sprite = BasicResourceManager.Instance.FactionHpSprites[_faction];
            // Update detail panel and interact ability panel
            detailInfoSlot.Show();
            detailInfoSlot.TrySwitchTarget(info.EntityPrefab);
            if (interactAbilitySlot.TrySwitchTarget(info.EntityPrefab))
                interactAbilitySlot.Show();
            else interactAbilitySlot.Hide();
        }

        private bool _preEnabled;

        private FactionTag _faction;
        private Entity _targetEntity = Entity.Null;
        private EntityManager _em;
        private EntityQuery _notPauseTag;

        private void OnEnable()
        {
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            _notPauseTag = _em.CreateEntityQuery(typeof(NotPauseTag));
        }

        private void OnDisable()
        {
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

            // Cannot construct even for one
            if (maxCount == 0)
            {
                if (_preEnabled)
                {
                    _preEnabled = false;
                    UIMathMethods.AnimateColorAsync(cannotColorChangeImage, cannotColorChangeImage.color,
                        cannotColor, 1f);
                    button!.enabled = false;
                }

                return;
            }

            if (!_preEnabled)
            {
                UIMathMethods.AnimateColorAsync(cannotColorChangeImage, cannotColorChangeImage.color,
                    Color.white, 1f);
                button!.enabled = true;
            }
        }
    }
}