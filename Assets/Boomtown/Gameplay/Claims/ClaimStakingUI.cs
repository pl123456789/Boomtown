using TMPro;
using UnityEngine;

/// <summary>
/// Displays the walk-anywhere staking prompt and short-lived feedback
/// messages (too narrow, already claimed, staked successfully, etc).
/// Same fade-based CanvasGroup pattern as GoldPanningUI/GeneralStoreUI.
/// </summary>
public sealed class ClaimStakingUI : MonoBehaviour
{
    [Header("Objects")]

    [SerializeField]
    private GameObject _promptObject;

    [SerializeField]
    private GameObject _messageObject;

    [Header("Components")]

    [SerializeField]
    private TMP_Text _promptText;

    [SerializeField]
    private TMP_Text _messageText;

    [SerializeField, Min(0.01f)]
    private float _fadeDuration = 0.16f;

    [SerializeField, Min(0.5f)]
    private float _messageDuration = 3.5f;

    private CanvasGroup _promptGroup;
    private CanvasGroup _messageGroup;
    private bool _promptVisible;
    private bool _messageVisible;
    private float _messageTimer;

    private void Awake()
    {
        _promptGroup = GetOrAddCanvasGroup(_promptObject);
        _messageGroup = GetOrAddCanvasGroup(_messageObject);
        SetGroupImmediate(_promptObject, _promptGroup, false);
        SetGroupImmediate(_messageObject, _messageGroup, false);
    }

    private void Update()
    {
        float fadeRate =
            _fadeDuration > 0f ? 1f / _fadeDuration : float.MaxValue;

        UpdateGroup(_promptObject, _promptGroup, _promptVisible, fadeRate);
        UpdateGroup(_messageObject, _messageGroup, _messageVisible, fadeRate);

        if (_messageVisible)
        {
            _messageTimer -= Time.unscaledDeltaTime;

            if (_messageTimer <= 0f)
            {
                _messageVisible = false;
            }
        }
    }

    public void ShowPrompt(
        string message)
    {
        if (_promptText != null)
        {
            _promptText.text = message;
        }

        _promptVisible = true;
    }

    /// <summary>
    /// Hides just the ambient prompt without touching a transient message
    /// that might be mid-fade -- used when another interaction (a trade
    /// panel, panning) currently has the player's attention instead.
    /// </summary>
    public void HidePrompt()
    {
        _promptVisible = false;
    }

    public void ShowMessage(
        string message)
    {
        if (_messageText != null)
        {
            _messageText.text = message;
        }

        _messageVisible = true;
        _messageTimer = _messageDuration;
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
