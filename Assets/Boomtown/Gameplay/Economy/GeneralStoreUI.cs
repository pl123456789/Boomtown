using TMPro;
using UnityEngine;

/// <summary>
/// Displays the General Store's walk-up prompt and trade panel. Mirrors
/// GoldPanningUI's fade-based show/hide pattern but without a progress bar
/// -- buying/selling grub is instant, not a timed activity.
/// </summary>
public sealed class GeneralStoreUI : MonoBehaviour
{
    [Header("Objects")]

    [SerializeField]
    private GameObject _promptObject;

    [SerializeField]
    private GameObject _panelObject;

    [Header("Components")]

    [SerializeField]
    private TMP_Text _promptText;

    [SerializeField]
    private TMP_Text _panelText;

    [SerializeField, Min(0.01f)]
    private float _fadeDuration = 0.16f;

    private CanvasGroup _promptGroup;
    private CanvasGroup _panelGroup;
    private bool _promptVisible;
    private bool _panelVisible;

    private void Awake()
    {
        _promptGroup = GetOrAddCanvasGroup(_promptObject);
        _panelGroup = GetOrAddCanvasGroup(_panelObject);
        SetGroupImmediate(_promptObject, _promptGroup, false);
        SetGroupImmediate(_panelObject, _panelGroup, false);
    }

    private void Update()
    {
        float fadeRate =
            _fadeDuration > 0f ? 1f / _fadeDuration : float.MaxValue;

        UpdateGroup(_promptObject, _promptGroup, _promptVisible, fadeRate);
        UpdateGroup(_panelObject, _panelGroup, _panelVisible, fadeRate);
    }

    public void ShowPrompt(
        string message)
    {
        if (_promptText != null)
        {
            _promptText.text = message;
        }

        _promptVisible = true;
        _panelVisible = false;
    }

    public void ShowPanel(
        string message)
    {
        if (_panelText != null)
        {
            _panelText.text = message;
        }

        _panelVisible = true;
        _promptVisible = false;
    }

    public void HideAll()
    {
        _promptVisible = false;
        _panelVisible = false;
    }

    private static CanvasGroup GetOrAddCanvasGroup(
        GameObject target)
    {
        if (target == null)
        {
            return null;
        }

        CanvasGroup group = target.GetComponent<CanvasGroup>();
        return group != null ? group : target.AddComponent<CanvasGroup>();
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

        if (!visible && group.alpha <= 0f && target.activeSelf)
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
