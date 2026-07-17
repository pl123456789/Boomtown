using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Boomtown.Gameplay.Prospecting
{
    public sealed class GoldPanningUI : MonoBehaviour
    {
        private enum DisplayState
        {
            Hidden,
            Prompt,
            Progress,
            Result
        }

        [Header("Objects")]

        [SerializeField]
        private GameObject panningPrompt;

        [SerializeField]
        private GameObject panningProgress;

        [SerializeField]
        private GameObject goldResultObject;

        [Header("Components")]

        [SerializeField]
        private TMP_Text promptText;

        [SerializeField]
        private Slider progressSlider;

        [SerializeField]
        private TMP_Text goldResultText;

        [Header("Smoothness")]

        [SerializeField, Min(0.01f)]
        private float fadeDuration = 0.16f;

        [SerializeField, Min(0.01f)]
        private float progressSmoothTime = 0.08f;

        private RectTransform progressFillRect;
        private CanvasGroup promptGroup;
        private CanvasGroup progressGroup;
        private CanvasGroup resultGroup;

        private DisplayState state;
        private float targetProgress;
        private float displayedProgress;
        private float progressVelocity;

        private void Awake()
        {
            CacheProgressFill();
            DisableSliderDriving();

            promptGroup = GetOrAddCanvasGroup(panningPrompt);
            progressGroup = GetOrAddCanvasGroup(panningProgress);
            resultGroup = GetOrAddCanvasGroup(goldResultObject);

            HideAllImmediate();
        }

        private void Update()
        {
            float fadeRate =
                fadeDuration > 0f
                    ? 1f / fadeDuration
                    : float.MaxValue;

            UpdateGroup(
                panningPrompt,
                promptGroup,
                state == DisplayState.Prompt,
                fadeRate);

            UpdateGroup(
                panningProgress,
                progressGroup,
                state == DisplayState.Progress,
                fadeRate);

            UpdateGroup(
                goldResultObject,
                resultGroup,
                state == DisplayState.Result,
                fadeRate);

            displayedProgress =
                Mathf.SmoothDamp(
                    displayedProgress,
                    targetProgress,
                    ref progressVelocity,
                    progressSmoothTime,
                    Mathf.Infinity,
                    Time.unscaledDeltaTime);

            if (Mathf.Abs(displayedProgress - targetProgress) < 0.001f)
            {
                displayedProgress = targetProgress;
            }

            SetProgressImmediate(displayedProgress);
        }

        public void HideAll()
        {
            state = DisplayState.Hidden;
            targetProgress = 0f;
        }

        public void ShowPrompt(string message)
        {
            SetPromptText(message);
            state = DisplayState.Prompt;
            targetProgress = 0f;
        }

        public void BeginProgress()
        {
            SetPromptText("Panning...");

            targetProgress = 0f;
            displayedProgress = 0f;
            progressVelocity = 0f;

            SetProgressImmediate(0f);
            state = DisplayState.Progress;
        }

        public void ShowProgress(float normalizedProgress)
        {
            targetProgress = Mathf.Clamp01(normalizedProgress);
            state = DisplayState.Progress;
        }

        public void ShowResult(string message)
        {
            if (goldResultText != null)
            {
                goldResultText.text = message;
                goldResultText.enabled = true;

                Color resultColour = goldResultText.color;
                resultColour.a = 1f;
                goldResultText.color = resultColour;
            }

            if (goldResultObject != null)
            {
                goldResultObject.transform.SetAsLastSibling();
            }

            targetProgress = 1f;
            displayedProgress = 1f;
            progressVelocity = 0f;
            SetProgressImmediate(1f);

            state = DisplayState.Result;
        }

        private void SetPromptText(string message)
        {
            if (promptText != null)
            {
                promptText.text = message;
                promptText.enabled = true;

                promptText.color = new Color32(
                    245,
                    232,
                    190,
                    255);
            }

            if (panningPrompt != null)
            {
                panningPrompt.transform.SetAsLastSibling();
            }
        }

        private void CacheProgressFill()
        {
            if (progressSlider != null)
            {
                progressFillRect = progressSlider.fillRect;
            }
        }

        private void DisableSliderDriving()
        {
            if (progressSlider == null)
            {
                return;
            }

            progressSlider.SetValueWithoutNotify(0f);
            progressSlider.enabled = false;
        }

        private void SetProgressImmediate(float normalizedProgress)
        {
            if (progressFillRect == null)
            {
                return;
            }

            float progress = Mathf.Clamp01(normalizedProgress);

            progressFillRect.anchorMin = new Vector2(0f, 0f);
            progressFillRect.anchorMax = new Vector2(progress, 1f);
            progressFillRect.pivot = new Vector2(0f, 0.5f);
            progressFillRect.offsetMin = Vector2.zero;
            progressFillRect.offsetMax = Vector2.zero;
        }

        private void HideAllImmediate()
        {
            state = DisplayState.Hidden;
            targetProgress = 0f;
            displayedProgress = 0f;
            progressVelocity = 0f;

            SetGroupImmediate(panningPrompt, promptGroup, false);
            SetGroupImmediate(panningProgress, progressGroup, false);
            SetGroupImmediate(goldResultObject, resultGroup, false);
            SetProgressImmediate(0f);
        }

        private static CanvasGroup GetOrAddCanvasGroup(GameObject target)
        {
            if (target == null)
            {
                return null;
            }

            CanvasGroup group = target.GetComponent<CanvasGroup>();

            if (group == null)
            {
                group = target.AddComponent<CanvasGroup>();
            }

            return group;
        }

        private static void UpdateGroup(
            GameObject target,
            CanvasGroup group,
            bool visible,
            float fadeRate)
        {
            if (target == null || group == null)
            {
                return;
            }

            if (visible && !target.activeSelf)
            {
                target.SetActive(true);
            }

            float targetAlpha = visible ? 1f : 0f;

            group.alpha = Mathf.MoveTowards(
                group.alpha,
                targetAlpha,
                fadeRate * Time.unscaledDeltaTime);

            group.interactable = visible && group.alpha >= 0.99f;
            group.blocksRaycasts = group.interactable;

            if (!visible &&
                group.alpha <= 0f &&
                target.activeSelf)
            {
                target.SetActive(false);
            }
        }

        private static void SetGroupImmediate(
            GameObject target,
            CanvasGroup group,
            bool visible)
        {
            if (target == null || group == null)
            {
                return;
            }

            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
            target.SetActive(visible);
        }
    }
}
