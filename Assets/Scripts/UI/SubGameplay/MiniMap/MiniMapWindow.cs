using System;
using SparFlame.Components.General;
using SparFlame.Systems.General.BasicControl;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SparFlame.UI.SubGameplay
{
    public class MiniMapWindow : MonoBehaviour
    {
        public RectTransform miniMapRect;
        public RectTransform miniMapSquareRect;
        public  Camera miniMapCamera;

        [SerializeField] private RawImage miniMapImage;
        [SerializeField] private GameObject miniMapPanel;
        public static MiniMapWindow Instance;
        public event Action OnEcsOnSquareDrag;

        private void Awake()
        {
            if (!Instance)
                Instance = this;
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            miniMapPanel.SetActive(false);
            var rt = new RenderTexture((int)miniMapImage.rectTransform.rect.width,
                (int)miniMapImage.rectTransform.rect.height, 16);
            miniMapCamera.targetTexture = rt;
            miniMapImage.texture = rt;
            GameController.Instance.OnEcsSwitchSubGameStatus += targetSubGameStatus =>
            {
                miniMapPanel.SetActive(targetSubGameStatus.SubGameStatus != SubGameStatus.None
                && targetSubGameStatus.SubGameStatus != SubGameStatus.PlayerCity);
            };
        }

        public void OnSquareDrag(BaseEventData data)
        {
            var ped = (PointerEventData)data;

            // Step 1: 获取屏幕坐标
            Vector2 screenPos = ped.position;

            // Step 2: 获取 MiniMap Rect 在屏幕中的位置和大小
            Vector3[] worldCorners = new Vector3[4];
            miniMapRect.GetWorldCorners(worldCorners);
            Vector2 miniMapScreenPos = new Vector2(worldCorners[0].x, worldCorners[0].y); // 左下角
            Vector2 miniMapSize = new Vector2(
                worldCorners[2].x - worldCorners[0].x,
                worldCorners[2].y - worldCorners[0].y);

            // Step 3: 计算鼠标在 minimap 中的相对位置（0~1）
            Vector2 relativePos = (screenPos - miniMapScreenPos);
            Vector2 normalized = new Vector2(
                Mathf.Clamp01(relativePos.x / miniMapSize.x),
                Mathf.Clamp01(relativePos.y / miniMapSize.y));

            // Step 4: 将归一化坐标映射到 miniMapRect 的 anchoredPosition 区域
            Vector2 anchoredPos = new Vector2(
                normalized.x * miniMapRect.rect.width,
                normalized.y * miniMapRect.rect.height);

            // Step 5: 设置 square 的 anchoredPosition
            miniMapSquareRect.anchoredPosition = anchoredPos;

            OnEcsOnSquareDrag?.Invoke();
        }
    }
}