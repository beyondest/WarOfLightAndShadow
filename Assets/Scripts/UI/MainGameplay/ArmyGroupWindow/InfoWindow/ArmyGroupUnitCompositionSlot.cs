using SparFlame.Components.MainGameplay;
using SparFlame.Systems.General.BasicControl;
using SparFlame.UI.General;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SparFlame.UI.MainGameplay
{
    public class ArmyGroupUnitCompositionSlot : MultiShowSlot
    {
        [SerializeField] private TMP_Text countText;
        [SerializeField] private TMP_Text unitName;
        [SerializeField] private Image tierImage;
        [SerializeField] private Image selectedSprite;
        
        public void SetTarget(in ArmyGroupUnitTypeData data)
        {
            var info = UnitWindowResourceManager.Instance.GetInfoByGeneralTypeAndIdx(data.UnitType,
                data.Id);
            button!.image.sprite =info.Sprite ;
            unitName.text = info.GameplayName;
            tierImage.sprite = BasicUIResourceManager.Instance.TierSprites[info.Tier];
            countText.text = data.Count.ToString();
            _data = data;
        }

        public ArmyGroupUnitTypeData GetData()
        {
            return _data;
        }

        public void ToggleSelected(bool isSelected)
        {
            selectedSprite.enabled = isSelected;
        }
        
        private ArmyGroupUnitTypeData _data;

        private void Start()
        {
            ToggleSelected(false);
        }
    }
}