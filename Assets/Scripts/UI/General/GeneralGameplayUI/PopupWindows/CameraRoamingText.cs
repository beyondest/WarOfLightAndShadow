using SparFlame.Systems.General.Camera;
using TMPro;
using UnityEngine;

namespace SparFlame.UI.General
{
    public class CameraRoamingText : MonoBehaviour
    {
        [SerializeField] private TMP_Text roamingText;
        [SerializeField] private GameObject panel;
        [SerializeField] private UIFadeInOut fadeInOut;


        private bool _previousTextIsEnemy;
        private void Start()
        {
            panel.SetActive(false);
            RoamingCameraController.Instance.OnStartRoamingCamera += StartRoamingCamera;
            RoamingCameraController.Instance.OnEndRoamingCamera += EndRoamingCamera;
            RoamingCameraController.Instance.OnShowEnemy += ShowEnemyText;
            RoamingCameraController.Instance.OnShowPlayer += ShowPlayerText;
            RoamingCameraController.Instance.OnSwitchToPlayer += SwitchToPlayer;
        }

        private void StartRoamingCamera()
        {
            panel.SetActive(true);
        }
        

        private void EndRoamingCamera()
        {
            panel.SetActive(false);
        }

        private void ShowEnemyText()
        {
            roamingText.text = "Enemy";
            fadeInOut.FadeIn();
        }

        private void SwitchToPlayer()
        {
            fadeInOut.FadeOut();
        }

        private void ShowPlayerText()
        {
            roamingText.text = "Player";
            fadeInOut.FadeIn();
        }
    }
}