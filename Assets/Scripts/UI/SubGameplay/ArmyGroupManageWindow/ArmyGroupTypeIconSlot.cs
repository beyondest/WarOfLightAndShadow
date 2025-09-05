using SparFlame.Components.MainGameplay;
using SparFlame.Systems.General.BasicControl;
using SparFlame.UI.General;

namespace SparFlame.UI.MainGameplay
{
    public class ArmyGroupTypeIconSlot : MultiShowSlot
    {
        
        public void SetTarget(ArmyGroupIconType type)
        {
            button!.image.sprite = ArmyGroupWindowResourceManager.Instance.ArmyGroupIcons[type];
        }
    }
}