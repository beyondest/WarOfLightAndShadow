using UnityEngine;
using UnityEngine.EventSystems;

namespace SparFlame.UI.General
{
    public class UIHoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private Vector3 _originalScale;
        private int _originalSiblingIndex;


        public float scaleTarget = 1.1f;
        public float scaleDuration = 5f;

        void Start()
        {
            _originalScale = transform.localScale;
            _originalSiblingIndex = transform.GetSiblingIndex();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            transform.SetAsLastSibling();

            UIMathMethods.AnimateFloatOverCurveAsyc(() => transform.localScale.x,
                f =>
                {
                    if(this == null) return;
                    transform.localScale = new Vector3(f, f, f);
                }, scaleDuration,
                AnimationCurve.EaseInOut(0f, 0f, 1f, 1f), scaleTarget);
       
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            transform.SetSiblingIndex(_originalSiblingIndex);
            UIMathMethods.AnimateFloatOverCurveAsyc(() => transform.localScale.x,
                f =>
                {
                    if(this == null) return;
                    transform.localScale = new Vector3(f, f, f);
                }, scaleDuration,
                AnimationCurve.EaseInOut(0f, 0f, 1f, 1f), _originalScale.x);
        }
    }
}