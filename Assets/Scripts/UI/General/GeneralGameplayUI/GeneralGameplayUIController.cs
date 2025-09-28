using SparFlame.Components.General;
using SparFlame.Systems.General.BasicControl;
using UnityEngine;

namespace SparFlame.UI.General.GeneralGameplayUI
{
    public class GeneralGameplayUIController : MonoBehaviour
    {
        private void Start()
        {
            GameController.Instance.OnSwitchGameStatus += (tar, cur)
                =>
            {
                WaitWindowController.Instance.SetEnable(!GameStatusUtils.IsInBattle(tar));
                WorldTimeClock.Instance.SetEnable(!GameStatusUtils.IsInBattle(tar));
            };
        }
    }
}