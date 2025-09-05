using SparFlame.Components.General;
using SparFlame.Systems.General.BasicControl;
using SparFlame.UI.General;
using UnityEngine;

namespace SparFlame.UI.MainGameplay
{
    public class ArmyGroupMulti2DSlot : MultiShowSlot
    {
        public void SetTarget(in ArmyGroupMulti2DRealTimeInfo info,
            FactionTag currentSelectFaction)
        {
            button!.image.sprite = ArmyGroupWindowResourceManager.Instance.ArmyGroupIcons[info.IconType];
            button.image.color = currentSelectFaction == FactionTag.Light ? Color.white : Color.black;
        }
    }
}