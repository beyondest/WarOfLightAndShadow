using System;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Interact;
using SparFlame.UI.General;
using SparFlame.Utils;
using TMPro;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace SparFlame.UI.SubGameplay
{
    public class ConstructBuildingSlot : MultiShowSlot,CustomDs.IResourceManager
    {
        [Header("General Settings")] [SerializeField]
        private Image tierIcon;

        [SerializeField] private TMP_Text gameplayNameText;
        [SerializeField] private TMP_Text hpText;
        [SerializeField] private Image hpImage;

        [Header("VFX")] [SerializeField] private Color cannotColor = Color.gray;
        [SerializeField] private Image cannotColorChangeImage;

        [FormerlySerializedAs("detailInfoSlot")] [Header("Sub slots")] [SerializeField] private ConstructDetailInfoPartWindow detailInfoPartWindow;
        [SerializeField] private InteractAbilitySlot interactAbilitySlot;

        public void SetTarget(in SpriteEntityInfo info)
        {
            //  Update basic ui info
            _targetEntity = info.EntityPrefab;
            gameplayNameText.text = info.GameplayName;
            tierIcon.sprite = BasicUIResourceManager.Instance.TierSprites[info.Tier];
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            _faction = _em.GetComponentData<SubGameplayGeneralAttr>(info.EntityPrefab).FactionTag;
            tierIcon.color = _faction == FactionTag.Ally ? Color.white : Color.black;
            hpText.text = _em.GetComponentData<StatData>(_targetEntity).maxValue.ToString();
            hpImage.sprite = BasicUIResourceManager.Instance.FactionHpSprites[_faction];
            // Update detail panel and interact ability panel
            detailInfoPartWindow.Show();
            detailInfoPartWindow.TrySwitchTarget(info.EntityPrefab);
            if (interactAbilitySlot.TrySwitchTarget(info.EntityPrefab))
                interactAbilitySlot.Show();
            else interactAbilitySlot.Hide();
        }

        private bool _preEnabled;
        private FactionTag _faction;
        private Entity _targetEntity = Entity.Null;
        private EntityManager _em;
        private EntityQuery _gamingTag;

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
        
        public bool IsInitialized => detailInfoPartWindow.IsInitialized;
        public float InitProgress => detailInfoPartWindow.InitProgress;
        public void LoadResources()
        {
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            _gamingTag = _em.CreateEntityQuery(typeof(SubGamingTag));
            detailInfoPartWindow.LoadResources();
        }
        public void UnloadResources()
        {
            detailInfoPartWindow.UnloadResources();
        }
    }
}