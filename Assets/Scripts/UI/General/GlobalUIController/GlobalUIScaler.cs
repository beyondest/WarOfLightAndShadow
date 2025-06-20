using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

namespace SparFlame.UI.General
{
    public class GlobalUIScaler : MonoBehaviour
    {
        public float scaleMultiplier = 1.1f;
        public float scaleDuration = 0.2f;

        private GameObject currentHoveredButton;
        private Dictionary<GameObject, Vector3> originalScales = new();
        private float t = 0f;

        void Update()
        {
            GameObject hoverObj = GetUIUnderMouse();
            if (hoverObj != currentHoveredButton)
            {
                if (currentHoveredButton)
                    ResetScale(currentHoveredButton);
                if (hoverObj)
                    StartScaleUp(hoverObj);

                currentHoveredButton = hoverObj;
                t = 0f;
            }

            if (currentHoveredButton)
            {
                AnimateScale(currentHoveredButton);
            }
        }

        GameObject GetUIUnderMouse()
        {
            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };
            
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            foreach (var result in results)
            {
                if (result.gameObject.GetComponent<Button>())
                    return result.gameObject;
            }

            return null;
        }

        void StartScaleUp(GameObject button)
        {
            if (!originalScales.ContainsKey(button))
                originalScales[button] = button.transform.localScale;
        }

        void AnimateScale(GameObject button)
        {
            if (!originalScales.ContainsKey(button)) return;

            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / scaleDuration);
            Vector3 from = originalScales[button];
            Vector3 to = from * scaleMultiplier;
            button.transform.localScale = Vector3.Lerp(from, to, progress);
        }

        void ResetScale(GameObject button)
        {
            if (originalScales.ContainsKey(button))
                button.transform.localScale = originalScales[button];
        }
    }
}