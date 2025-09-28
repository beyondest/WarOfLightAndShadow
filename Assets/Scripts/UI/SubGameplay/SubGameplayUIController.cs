using SparFlame.Components.General;
using SparFlame.Systems.General.BasicControl;
using SparFlame.UI.SubGameplay.StaticWindows.Buttons;
using UnityEngine;

namespace SparFlame.UI.SubGameplay
{
    public class SubGameplayUIController : MonoBehaviour
    {
        [SerializeField] private GameObject subGameplayUI;

        private void Start()
        {
            subGameplayUI.SetActive(false);
            GameController.Instance.OnSwitchGameStatus += (tar, cur)
                =>
            {
                subGameplayUI.SetActive(tar.SubGameStatus != SubGameStatus.None);
                InfoWindowController.Instance.Hide();
                SubGameplayStaticButtonManager.Instance.TogglePlayerCityUI(
                    tar.SubGameStatus == SubGameStatus.PlayerCity);
                SubGameplayStaticButtonManager.Instance.ToggleBattleUI(GameStatusUtils.IsInBattle(tar));
            };
        }
    }
}