using System;
using System.Collections;
using UnityEngine;

namespace EndlessHallway.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class FadeController : MonoBehaviour
    {
        public static FadeController Instance { get; private set; }

        private CanvasGroup canvasGroup;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }

        public void SetAlpha(float alpha)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = alpha;
                canvasGroup.blocksRaycasts = alpha > 0.01f;
            }
        }

        public void SetColor(Color color)
        {
            var img = GetComponent<UnityEngine.UI.Image>();
            if (img != null)
            {
                img.color = color;
            }
        }

        public IEnumerator FadeOutRoutine(float duration = 1.0f)
        {
            float elapsed = 0f;
            float startAlpha = canvasGroup.alpha;
            canvasGroup.blocksRaycasts = true;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, elapsed / duration);
                yield return null;
            }
            canvasGroup.alpha = 1f;
        }

        public IEnumerator FadeInRoutine(float duration = 1.0f)
        {
            float elapsed = 0f;
            float startAlpha = canvasGroup.alpha;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / duration);
                yield return null;
            }
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }

        public void FadeIn(float duration = 1.0f)
        {
            StartCoroutine(FadeInRoutine(duration));
        }

        public void FadeOut(float duration = 1.0f)
        {
            StartCoroutine(FadeOutRoutine(duration));
        }

        public void FadeOutAndIn(float outDuration, float pauseDuration, float inDuration, Action onDark = null)
        {
            StartCoroutine(FadeSequence(outDuration, pauseDuration, inDuration, onDark));
        }

        private IEnumerator FadeSequence(float outDuration, float pauseDuration, float inDuration, Action onDark)
        {
            yield return StartCoroutine(FadeOutRoutine(outDuration));
            onDark?.Invoke();
            yield return new WaitForSecondsRealtime(pauseDuration);
            yield return StartCoroutine(FadeInRoutine(inDuration));
        }
    }
}
