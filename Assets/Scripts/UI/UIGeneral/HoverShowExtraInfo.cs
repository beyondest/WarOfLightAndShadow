using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SparFlame.UI.General
{

    public class HoverShowExtraInfoText : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public TMP_Text extraInfoText;

        private void Start()
        {
            if (extraInfoText != null)
                extraInfoText.gameObject.SetActive(false); 
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (extraInfoText != null)
                extraInfoText.gameObject.SetActive(true); 
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (extraInfoText != null)
                extraInfoText.gameObject.SetActive(false); 
        }
    }
}