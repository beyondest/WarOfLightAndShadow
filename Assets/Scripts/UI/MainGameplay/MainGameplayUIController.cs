using System;
using SparFlame.Components.General;
using SparFlame.Systems.General.BasicControl;
using UnityEngine;

namespace SparFlame.UI.MainGameplay
{
    public class MainGameplayUIController : MonoBehaviour
    {
        [SerializeField] private GameObject mainGameplayUI;


        private void Start()
        {
            mainGameplayUI.SetActive(false);
            GameController.Instance.OnSwitchGameStatus += (tar, cur)
                =>
            {
                mainGameplayUI.SetActive(tar.SubGameStatus == SubGameStatus.None);
                MainGameplayInfoWindowController.Instance.Hide();
            };
        }
    }
}