using TMPro;
using UnityEngine;

/// <summary>
/// Displays the Gold Commissioner's Office's walk-up prompt, trade panel,
/// and short-lived transaction feedback (insufficient gold, nothing
/// staked, registered successfully, etc). Same fade-based CanvasGroup
/// pattern as GeneralStoreUI, plus a transient message group like
/// ClaimStakingUI since this office reports success/failure per action.
/// </summary>
public sealed class GoldCommissionerOfficeUI : MonoBehaviour
{
    [Header("Objects")]

    [SerializeField]
    private GameObject _promptObject;

    [SerializeField]
    private GameObject _panelObject;

    [SerializeField]
    private GameObject _messageObject;

    [Header("Components")]

    [SerializeField]
    private TMP_Text _promptText;

    [SerializeField]
    private TMP_Text _panelText;

    [SerializeField]
    private TMP_Text _messageText;

    [SerializeField, Min(0.01f)]
    private float _fadeDuration = 0.16f;

    [SerializeField, Min(0.5f)]
    private float _messageDuration = 3.5f;

    private CanvasGroup _promptGroup;
    private CanvasGroup _panelGroup;
    private CanvasGroup _messageGroup;
    private bool _promptVisible;
    private bool _panelVisible;
    private bool _messageVisible;
    private float _messageTimer;

    private void Awake()
    {
        _promptGroup = GetOrAddCanvasGroup(_promptObject);
        _panelGroup = GetOrAddCanvasGroup(_panelObject);
        _messageGroup = GetOrAddCanvasGroup(_messageObject);
        SetGroupImmediate(_promptObject, _promptGroup, false);
        SetGroupImmediate(_panelObject, _panelGroup, false);
        SetGroupImmediate(_messageObject, _messageGroup, false);
    }

    private void Update()
    {
        float fadeRate =
            _fadeDuration > 0f ? 1f / _fadeDuration : float.MaxValue;

        UpdateGroup(_promptObject, _promptGroup, _promptVisible, fadeRate);
        UpdateGroup(_panelObject, _panelGroup, _panelVisible, fadeRate);
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

    public void HideAll()
    {
        _promptVisible = false;
        _panelVisible = false;
        _messageVisible = false;
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
