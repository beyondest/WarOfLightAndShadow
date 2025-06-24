using SparFlame.Components.General;
using SparFlame.Systems.General.BasicControl;
using SparFlame.UI.General;
using UnityEngine;

namespace SparFlame.UI.SubGameplay.StaticWindows.Buttons
{
    
    public class ButtonBackToMainWorld : ButtonUtils.SelfButton
    {
        
        [SerializeField] private GameObject backToMainWorldPanel;

        protected override void Awake()
        {
            base.Awake();
            GameController.Instance.OnSwitchGameStatusForSystems += status =>
            {
                backToMainWorldPanel.SetActive(status.SubGameStatus == SubGameStatus.PlayerCity);
            };
        }

  

        public override void OnClick()
        {
            GameController.Instance.BackToMainWorld();
        }
        
    }
}