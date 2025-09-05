using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace SparFlame.UI.General
{
    public class LeftRightTrigger : MonoBehaviour
    {
  
        [Header("Events triggered when cursor is in left half of screen")]
        public UnityEvent onCursorLeftSide;

        [Header("Events triggered when cursor is in right half of screen")]
        public UnityEvent onCursorRightSide;

        private bool lastFrameLeft = false;

        void Update()
        {
            Vector3 mousePos = Input.mousePosition;
            bool isLeft = mousePos.x < Screen.width / 2f;

            // 只在位置变化时触发一次（防止每帧多次触发）
            if (isLeft != lastFrameLeft)
            {
                lastFrameLeft = isLeft;

                if (isLeft)
                    onCursorLeftSide?.Invoke();
                else
                    onCursorRightSide?.Invoke();
            }
        }

    }
}