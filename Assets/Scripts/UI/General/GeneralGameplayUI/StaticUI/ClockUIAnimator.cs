using UnityEngine;
using DG.Tweening;
using SparFlame.Systems.General.BasicControl;
using SparFlame.UI.General;

public class ClockUIAnimator : UIGeneralAnimator
{
    [Header("Animation Parameters")]
    public Vector3 targetScale = new(2f, 2f, 2f);

    public void PlayToCenter()
    {
        if (HasChanged) return;
        CurrentSeq?.Kill();

        var screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        CurrentSeq = DOTween.Sequence();

        foreach (var rt in AnimatedRects)
        {
            if (!rt) continue;

            var targetAnchored = GetAnchoredPositionForScreenPoint(rt, screenCenter);
            // 使用 DOTween.To 来动画 anchoredPosition 与 localScale（避免依赖扩展方法）
            var posTween = DOTween.To(() => rt.anchoredPosition, x => rt.anchoredPosition = x, targetAnchored, duration).SetEase(moveEase);
            var scaleTween = DOTween.To(() => rt.localScale, x => rt.localScale = x, targetScale, duration).SetEase(scaleEase);

            // 加入 sequence 以并行播放
            CurrentSeq.Join(posTween);
            CurrentSeq.Join(scaleTween);
        }

        CurrentSeq.OnComplete(() => HasChanged = true);
        CurrentSeq.Play();
    }

    protected override void Start()
    {
        base.Start();
        GameController.Instance.OnStartWait += PlayToCenter;
        GameController.Instance.OnEndWait += ResetToOriginal;
    }
}
