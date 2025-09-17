using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

namespace SparFlame.UI.General
{

public class UIAnimator : MonoBehaviour
{
    private readonly Dictionary<RectTransform, Coroutine> activeScaleAnimations = new();
    private readonly Dictionary<Image, Coroutine> activeColorAnimations = new();

    public static UIAnimator Instance;
    #region 放大缩小动画
    public void PlayPopAnimation(RectTransform target, float scaleMultiplier, float duration)
    {
        if (!target) return;

        if (activeScaleAnimations.TryGetValue(target, out Coroutine running))
        {
            StopCoroutine(running);
            activeScaleAnimations.Remove(target);
            target.localScale = Vector3.one; // 重置为初始大小（可改为保存原始值）
        }

        Coroutine newAnim = StartCoroutine(DoPopAnimation(target, scaleMultiplier, duration));
        activeScaleAnimations[target] = newAnim;
    }

    private IEnumerator DoPopAnimation(RectTransform target, float scaleMultiplier, float duration)
    {
        Vector3 originalScale = Vector3.one;
        Vector3 targetScale = originalScale * scaleMultiplier;

        float halfDuration = duration / 2f;
        float t = 0f;

        // 放大
        while (t < halfDuration)
        {
            t += Time.deltaTime;
            target.localScale = Vector3.Lerp(originalScale, targetScale, t / halfDuration);
            yield return null;
        }

        // 缩小
        t = 0f;
        while (t < halfDuration)
        {
            t += Time.deltaTime;
            target.localScale = Vector3.Lerp(targetScale, originalScale, t / halfDuration);
            yield return null;
        }

        target.localScale = originalScale;
        activeScaleAnimations.Remove(target);
    }
    #endregion

    #region 颜色闪烁动画
    /// <summary>
    /// 让Image从当前颜色闪烁到指定颜色，再恢复
    /// </summary>
    /// <param name="image">目标Image</param>
    /// <param name="flashColor">闪烁颜色</param>
    /// <param name="duration">总时长</param>
    public void PlayFlashAnimation(Image image, Color flashColor, float duration)
    {
        if (!image) return;

        if (activeColorAnimations.TryGetValue(image, out Coroutine running))
        {
            StopCoroutine(running);
            activeColorAnimations.Remove(image);
            // 恢复到原色
            image.color = Color.white; // 这里等于没变 → 需要保存初始颜色
        }

        Coroutine newAnim = StartCoroutine(DoFlashAnimation(image, flashColor, duration));
        activeColorAnimations[image] = newAnim;
    }

    private IEnumerator DoFlashAnimation(Image image, Color flashColor, float duration)
    {
        Color originalColor = image.color;
        float halfDuration = duration / 2f;
        float t = 0f;

        // 变到目标色
        while (t < halfDuration)
        {
            t += Time.deltaTime;
            image.color = Color.Lerp(originalColor, flashColor, t / halfDuration);
            yield return null;
        }

        // 再变回原色
        t = 0f;
        while (t < halfDuration)
        {
            t += Time.deltaTime;
            image.color = Color.Lerp(flashColor, originalColor, t / halfDuration);
            yield return null;
        }

        image.color = originalColor;
        activeColorAnimations.Remove(image);
    }
    #endregion

    private void Awake()
    {
        if(!Instance)
            Instance = this;
        else
            Destroy(gameObject);
    }
}

   
}