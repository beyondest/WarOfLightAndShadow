using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Systems.General.BasicControl;
using SparFlame.UI.General;
using TMPro;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace SparFlame.UI.MainGameplay
{
    public class CityGarrisonSlot3D : MultiShowSlot
    {
        public Image armyGroupIcon;
        // public TMP_Text armyGroupName;
        public GameObject panel;
        public Image filledHpLight;
        public Image filledHpDark;
        public RectTransform hpImageTransformLight;
        public RectTransform hpImageTransformDark;
        public Color darkColor;
        public Color lightColor;
        public GameObject hpLight;
        public GameObject hpDark;
        
        private float _hpInitHei;
        private float _hpInitWidth;

        private FactionTag _currentFaction;
        private void Awake()
        {
            _hpInitHei = hpImageTransformLight.rect.height;
            _hpInitWidth = hpImageTransformLight.rect.width;
        }

        public void SetTarget(Entity armyGroup)
        {
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            var armyGroupAttr = em.GetComponentData<ArmyGroupAttr>(armyGroup);
            _currentFaction = em.GetComponentData<MainGameplayGeneralAttr>(armyGroup).faction;

            armyGroupIcon.sprite = ArmyGroupWindowResourceManager.Instance.ArmyGroupIcons[armyGroupAttr.iconType];
            armyGroupIcon.color = _currentFaction == FactionTag.Dark ? darkColor : lightColor;
            // armyGroupName.text = armyGroupAttr.gameplayName.ToString();
            hpDark.SetActive(_currentFaction == FactionTag.Dark);
            hpLight.SetActive(_currentFaction == FactionTag.Light);
        }

        public void UpdateHp(float hpRatio)
        {
            var filledImage = _currentFaction == FactionTag.Dark ? filledHpDark : filledHpLight;
            filledImage.fillAmount = hpRatio;
        }
        
        public void UpdateSize(float ratio)
        {

            // 计算缩放后的大小
            float newW = _hpInitWidth * ratio;
            float newH = _hpInitHei * ratio;

            var rect = _currentFaction == FactionTag.Dark ? hpImageTransformDark : hpImageTransformLight;
            // 更新 RectTransform
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newW);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, newH);
        }
    }
}