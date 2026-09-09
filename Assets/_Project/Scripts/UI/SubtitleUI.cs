using System.Collections;
using UnityEngine;
using TMPro;
using EndlessHallway.Core;

namespace EndlessHallway.UI
{
    public class SubtitleUI : MonoBehaviour
    {
        private static SubtitleUI instance;
        public static SubtitleUI Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindAnyObjectByType<SubtitleUI>();
                    if (instance == null)
                    {
                        var canvas = FindAnyObjectByType<Canvas>();
                        if (canvas != null)
                        {
                            instance = canvas.gameObject.AddComponent<SubtitleUI>();
                        }
                    }
                }
                return instance;
            }
        }

        [Header("UI Elements")]
        [SerializeField] private GameObject subtitleRoot;
        [SerializeField] private TextMeshProUGUI subtitleText;

        private Coroutine hideCoroutine;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;

            EnsureUI();
            if (subtitleRoot != null) subtitleRoot.SetActive(false);
        }

        private void EnsureUI()
        {
            if (subtitleRoot != null && subtitleText != null) return;

            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            var existing = canvas.transform.Find("SubtitlePanel");
            if (existing != null)
            {
                subtitleRoot = existing.gameObject;
                subtitleText = subtitleRoot.GetComponentInChildren<TextMeshProUGUI>();
                return;
            }

            var panelObj = new GameObject("SubtitlePanel");
            panelObj.transform.SetParent(canvas.transform, false);
            var rect = panelObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.18f, 0.04f);
            rect.anchorMax = new Vector2(0.82f, 0.12f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var img = panelObj.AddComponent<UnityEngine.UI.Image>();
            img.color = new Color(0.04f, 0.04f, 0.05f, 0.75f);

            var textObj = new GameObject("SubtitleText");
            textObj.transform.SetParent(panelObj.transform, false);
            var tRect = textObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.offsetMin = new Vector2(16, 4);
            tRect.offsetMax = new Vector2(-16, -4);

            var tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 18f;
            tmp.color = new Color(0.96f, 0.94f, 0.86f, 1f);
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.text = "";

            subtitleRoot = panelObj;
            subtitleText = tmp;
            subtitleRoot.SetActive(false);
        }

        public void ShowSubtitle(string text, float duration = 3.5f)
        {
            if (SettingsManager.Instance != null && !SettingsManager.Instance.SubtitlesEnabled)
            {
                return;
            }

            if (subtitleText != null)
            {
                subtitleText.text = text;
            }

            if (subtitleRoot != null)
            {
                subtitleRoot.SetActive(true);
            }

            if (hideCoroutine != null)
            {
                StopCoroutine(hideCoroutine);
            }
            hideCoroutine = StartCoroutine(HideRoutine(duration));
        }

        public void HideSubtitle()
        {
            if (hideCoroutine != null)
            {
                StopCoroutine(hideCoroutine);
                hideCoroutine = null;
            }

            if (subtitleRoot != null)
            {
                subtitleRoot.SetActive(false);
            }
            if (subtitleText != null)
            {
                subtitleText.text = "";
            }
        }

        private IEnumerator HideRoutine(float duration)
        {
            yield return new WaitForSeconds(duration);
            HideSubtitle();
        }
    }
}
