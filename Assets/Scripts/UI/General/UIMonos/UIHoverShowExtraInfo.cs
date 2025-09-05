using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SparFlame.UI.General
{
    public class HoverShowExtraInfoText : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public List<TMP_Text> extraInfoTexts;

        private void Start()
        {
            foreach (var extraInfoText in extraInfoTexts)
            {
                if (extraInfoText != null)
                    extraInfoText.gameObject.SetActive(false);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            foreach (var extraInfoText in extraInfoTexts)
            {
                extraInfoText.gameObject.SetActive(true);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            foreach (var extraInfoText in extraInfoTexts)
            {
                extraInfoText.gameObject.SetActive(false);
            }
        }
    }
}