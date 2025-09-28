using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

namespace SparFlame.UI.General
{
    public class UIGeneralAnimator : MonoBehaviour
    {
        [Header("Manually add rects you want to animate")]
        public List<RectTransform> clockPieces = new();
        
        [Header("Root Panel")] public RectTransform panel;
        public bool includeChildren = true;
        public bool includeSelf = true;
        [Header("Animation Parameters")] public float duration = 0.45f;
        public Ease moveEase = Ease.OutQuad;
        public Ease scaleEase = Ease.OutBack;


        public void ResetToOriginal()
        {
            if (!HasChanged) return;

            CurrentSeq?.Kill();
            CurrentSeq = DOTween.Sequence();

            foreach (var rt in AnimatedRects)
            {
                if (!rt) continue;
                var origPos = originalAnchoredPos.TryGetValue(rt, out var po) ? po : rt.anchoredPosition;
                var origScale = originalLocalScale.TryGetValue(rt, out var value) ? value : rt.localScale;

                var posTween = DOTween.To(() => rt.anchoredPosition, x => rt.anchoredPosition = x, origPos, duration)
                    .SetEase(moveEase);
                var scaleTween = DOTween.To(() => rt.localScale, x => rt.localScale = x, origScale, duration)
                    .SetEase(scaleEase);

                CurrentSeq.Join(posTween);
                CurrentSeq.Join(scaleTween);
            }

            CurrentSeq.OnComplete(() => HasChanged = false);
            CurrentSeq.Play();
        }

        // Cache
        protected readonly List<RectTransform> AnimatedRects = new();
        private readonly Dictionary<RectTransform, Vector2> originalAnchoredPos = new();
        private readonly Dictionary<RectTransform, Vector3> originalLocalScale = new();

        protected Sequence CurrentSeq;
        protected bool HasChanged;


        protected virtual void Start()
        {
            CollectAnimatedRects();
            RecordOriginalTransforms();
        }


        protected void CollectAnimatedRects()
        {
            var set = new HashSet<RectTransform>();

            // 手动列出来的 clock 片段
            foreach (var rt in clockPieces)
                if (rt)
                    set.Add(rt);
            if (includeSelf) set.Add(panel.GetComponent<RectTransform>());
            // panel 下的 UI 元素（Image, Text, TextMeshProUGUI）
            if (includeChildren)
            {
                var images = panel.GetComponentsInChildren<Image>(true);
                foreach (var img in images)
                    if (img && img.rectTransform)
                        set.Add(img.rectTransform);

                var texts = panel.GetComponentsInChildren<Text>(true);
                foreach (var t in texts)
                    if (t && t.rectTransform)
                        set.Add(t.rectTransform);

                var tmpPros = panel.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var tt in tmpPros)
                    if (tt && tt.rectTransform)
                        set.Add(tt.rectTransform);
            }

            AnimatedRects.Clear();
            AnimatedRects.AddRange(set);
        }

        protected void RecordOriginalTransforms()
        {
            originalAnchoredPos.Clear();
            originalLocalScale.Clear();
            foreach (var rt in AnimatedRects)
            {
                if (!rt) continue;
                originalAnchoredPos[rt] = rt.anchoredPosition;
                originalLocalScale[rt] = rt.localScale;
            }
        }

        // 计算某 RectTransform 在其父坐标系下，对应屏幕点（screenPoint）的 anchoredPosition
        protected Vector2 GetAnchoredPositionForScreenPoint(RectTransform rt, Vector2 screenPoint)
        {
            var parent = rt.parent as RectTransform;
            if (!parent) return rt.anchoredPosition; // 退化处理

            var canvas = rt.GetComponentInParent<Canvas>();
            Camera cam = null;
            if (canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                cam = canvas.worldCamera;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPoint, cam, out var localPoint);
            return localPoint;
        }
    }
}