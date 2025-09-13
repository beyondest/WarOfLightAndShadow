using SparFlame.Components.General;
using SparFlame.Systems.General.BasicControl;
using UnityEngine;

namespace SparFlame.UI.SubGameplay.StaticWindows.Buttons
{
    public class ButtonBackToMainWorld : MonoBehaviour
    {
        [SerializeField] private GameObject backToMainWorldPanel;
        public static ButtonBackToMainWorld Instance;

        private void Awake()
        {
            if (!Instance)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            GameController.Instance.OnSwitchGameStatusForSystems += status =>
            {
                backToMainWorldPanel.SetActive(status.SubGameStatus == SubGameStatus.PlayerCity);
            };
        }


        public void OnClick()
        {
            GameController.Instance.BackToMainWorld(false);
        }
    }
}