using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Boomtown.Gameplay.Prospecting
{
    public sealed class GoldPanningUI : MonoBehaviour
    {
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

        private RectTransform progressFillRect;

        private void Awake()
        {
            CacheProgressFill();
            DisableSliderDriving();
            HideAll();
        }

        public void HideAll()
        {
            SetActive(panningPrompt, false);
            SetActive(panningProgress, false);
            SetActive(goldResultObject, false);

            SetProgressImmediate(0f);
        }

        public void ShowPrompt(string message)
        {
            PreparePrompt(message);

            SetActive(panningProgress, false);
            SetActive(goldResultObject, false);
        }

        public void BeginProgress()
        {
            SetActive(panningProgress, true);
            SetActive(goldResultObject, false);

            PreparePrompt("Panning...");

            SetProgressImmediate(0f);
        }

        public void ShowProgress(float normalizedProgress)
        {
            SetActive(panningProgress, true);
            SetActive(goldResultObject, false);

            PreparePrompt("Panning...");

            SetProgressImmediate(normalizedProgress);
        }

        public void ShowResult(string message)
        {
            if (goldResultText != null)
            {
                goldResultText.text = message;
                goldResultText.enabled = true;

                Color resultColour =
                    goldResultText.color;

                resultColour.a = 1f;
                goldResultText.color =
                    resultColour;
            }

            SetActive(panningPrompt, false);
            SetActive(panningProgress, false);
            SetActive(goldResultObject, true);

            if (goldResultObject != null)
            {
                goldResultObject.transform.SetAsLastSibling();
            }

            SetProgressImmediate(0f);
        }

        private void PreparePrompt(string message)
        {
            SetActive(panningPrompt, true);

            if (promptText == null)
            {
                return;
            }

            promptText.text = message;
            promptText.enabled = true;

            // Warm parchment colour
            promptText.color = new Color32(
                245,
                232,
                190,
                255);

            panningPrompt.transform.SetAsLastSibling();
        }

        private void CacheProgressFill()
        {
            if (progressSlider != null)
            {
                progressFillRect =
                    progressSlider.fillRect;
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

        private void SetProgressImmediate(
            float normalizedProgress)
        {
            if (progressFillRect == null)
            {
                return;
            }

            float progress =
                Mathf.Clamp01(
                    normalizedProgress);

            progressFillRect.anchorMin =
                new Vector2(0f, 0f);

            progressFillRect.anchorMax =
                new Vector2(progress, 1f);

            progressFillRect.pivot =
                new Vector2(0f, 0.5f);

            progressFillRect.offsetMin =
                Vector2.zero;

            progressFillRect.offsetMax =
                Vector2.zero;
        }

        private static void SetActive(
            GameObject target,
            bool active)
        {
            if (target != null &&
                target.activeSelf != active)
            {
                target.SetActive(active);
            }
        }
    }
}