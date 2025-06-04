using SparFlame.GamePlaySystem.General;
using SparFlame.UI.General;
using UnityEngine;
using UnityEngine.UI;

namespace SparFlame.UI.GamePlay
{
    public class Unit2DSlot : MultiShowSlot
    {
        public Image hpFilled;
        public Image tierImage;
        public Image hpBlank;
        public Image tierBg;
        public void SetTarget(in UnitRealTimeInfo info,
            FactionTag currentSelectFaction,
            float maxTierF)
        {
            button!.image.sprite = UnitWindowResourceManager.Instance.UnitGeneralTypeSprites[info.UnitType];
            hpFilled.sprite = BasicUIResourceManager.Instance.FactionHpFillSprites[currentSelectFaction];
            hpBlank.sprite = BasicUIResourceManager.Instance.FactionHpBlankSprites[currentSelectFaction];
            hpFilled.fillAmount = info.HpRatio;
            var tier = (int)info.Tier - 2;
            tierImage.fillAmount = tier / maxTierF;
            tierBg.color = currentSelectFaction == FactionTag.Ally ? Color.white : Color.black;
            button.image.color = currentSelectFaction == FactionTag.Ally ? Color.white : Color.black;
        }
    }
}