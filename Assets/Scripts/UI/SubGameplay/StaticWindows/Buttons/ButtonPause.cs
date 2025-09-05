using SparFlame.Systems.General.BasicControl;
using SparFlame.UI.General;

namespace SparFlame.UI.SubGameplay.StaticWindows.Buttons
{
    public class ButtonPause : ButtonUtils.SelfButton
    {
        public override void OnClick()
        {
            MenuOutController.Instance.ShowPauseMenu();
            GameController.Instance.PauseGame(false);
        }
    }
}