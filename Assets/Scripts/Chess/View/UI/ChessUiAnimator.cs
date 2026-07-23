using System.Collections;
using UnityEngine;

namespace Chess.View.UI
{
    public static class ChessUiAnimator
    {
        public static IEnumerator PopIn(CanvasGroup group, RectTransform rect, float duration = 0.35f)
        {
            if (group == null)
                yield break;

            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            var startScale = Vector3.one * 0.86f;
            var endScale = Vector3.one;
            if (rect != null)
                rect.localScale = startScale;

            var elapsed = 0f;
            duration = Mathf.Max(0.05f, duration);
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                group.alpha = t;
                if (rect != null)
                    rect.localScale = Vector3.Lerp(startScale, endScale, t);
                yield return null;
            }

            group.alpha = 1f;
            if (rect != null)
                rect.localScale = endScale;
            group.interactable = true;
            group.blocksRaycasts = true;
        }

        public static IEnumerator PopOut(CanvasGroup group, RectTransform rect, float duration = 0.2f)
        {
            if (group == null)
                yield break;

            group.interactable = false;
            group.blocksRaycasts = false;
            var startScale = rect != null ? rect.localScale : Vector3.one;
            var elapsed = 0f;
            duration = Mathf.Max(0.05f, duration);
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                group.alpha = 1f - t;
                if (rect != null)
                    rect.localScale = Vector3.Lerp(startScale, Vector3.one * 0.9f, t);
                yield return null;
            }

            group.alpha = 0f;
            group.gameObject.SetActive(false);
        }

        public static IEnumerator PunchScale(RectTransform rect, float peak = 1.08f, float duration = 0.28f)
        {
            if (rect == null)
                yield break;

            var baseScale = Vector3.one;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = elapsed / duration;
                var wave = Mathf.Sin(t * Mathf.PI);
                rect.localScale = baseScale * Mathf.Lerp(1f, peak, wave);
                yield return null;
            }

            rect.localScale = baseScale;
        }
    }
}
