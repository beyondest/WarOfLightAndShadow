using SparFlame.Systems.General.BasicControl;
using SparFlame.Systems.General.Camera;
using UnityEngine;
using UnityEngine.UI;

namespace SparFlame.UI.General
{
    public class GeneralModalWindowController : MonoBehaviour
    {
        [SerializeField]private Image image;
        [SerializeField] private UIFadeInOut fadeInOut;
        public static GeneralModalWindowController Instance;
        private void Awake()
        {
            if(!Instance)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private void Start()
        {
            GameController.Instance.OnEndWait += Hide;
            RoamingCameraController.Instance.OnStartRoamingCamera += Show;
            RoamingCameraController.Instance.OnEndRoamingCamera += Hide;
        }

        public void Show()
        {
            image.raycastTarget = true;
            fadeInOut.FadeIn();
        }

        public void Hide()
        {
            image.raycastTarget = false;
            fadeInOut.FadeOut();
        }
    }
}