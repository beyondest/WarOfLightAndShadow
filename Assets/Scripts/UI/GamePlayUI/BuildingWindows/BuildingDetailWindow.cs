using SparFlame.GamePlaySystem.Building;
using SparFlame.UI.General;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SparFlame.UI.GamePlay.UI.GamePlayUI.BuildingWindows
{
    public class BuildingDetailWindow : UIUtils.UIWindow
    {
        public GameObject buildingDetailPanel;
        public TMP_Text buildingType;
        public TMP_Text buildingHp;
        public Image buildingIcon;
        public Image buildingHpIcon;
        
        
        public override void Show(Vector2? pos = null)
        {
            throw new System.NotImplementedException();
        }

        public override void Hide()
        {
            throw new System.NotImplementedException();
        }

        public override bool IsOpened()
        {
            throw new System.NotImplementedException();
        }
        
    }
}