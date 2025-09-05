using UnityEngine;

namespace SparFlame.UI.SubGameplay.StaticWindows.Buttons
{
     


    public class TutorialWindow : MonoBehaviour
    {
        [SerializeField] private GameObject tutorialPanel;
        [SerializeField] private GameObject controlPanel;
        [SerializeField] private GameObject infoPanel;


        private void Start()
        {
            controlPanel.SetActive(false);
            infoPanel.SetActive(false);
            tutorialPanel.SetActive(false);
        }

        public void OnClickControlButton()
        {
            controlPanel.SetActive(true);
            infoPanel.SetActive(false);
        }

        public void OnClickInfoButton()
        {
            infoPanel.SetActive(true);
            controlPanel.SetActive(false);
        }

        public void OnClickTutorialExit()
        {
            tutorialPanel.SetActive(false);
        }
        public  void OnClickTutorialEnter()
        {
            tutorialPanel.SetActive(true);
            OnClickControlButton();
        }
        
    }
}