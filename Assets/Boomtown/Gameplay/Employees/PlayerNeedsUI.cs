using TMPro;
using UnityEngine;

/// <summary>
/// Displays Bill's personal needs menu ("Eat Some Grub" / "Get Some
/// Shuteye") and a brief "resting" indicator while shuteye is in progress.
/// Same fade-based show/hide pattern as GoldPanningUI and GeneralStoreUI.
/// </summary>
public sealed class PlayerNeedsUI : MonoBehaviour
{
    [Header("Objects")]

    [SerializeField]
    private GameObject _menuObject;

    [SerializeField]
    private GameObject _restingObject;

    [Header("Components")]

    [SerializeField]
    private TMP_Text _menuText;

    [SerializeField, Min(0.01f)]
    private float _fadeDuration = 0.16f;

    private CanvasGroup _menuGroup;
    private CanvasGroup _restingGroup;
    private bool _menuVisible;
    private bool _restingVisible;

    private void Awake()
    {
        _menuGroup = GetOrAddCanvasGroup(_menuObject);
        _restingGroup = GetOrAddCanvasGroup(_restingObject);
        SetGroupImmediate(_menuObject, _menuGroup, false);
        SetGroupImmediate(_restingObject, _restingGroup, false);
    }

    private void Update()
    {
        float fadeRate =
            _fadeDuration > 0f ? 1f / _fadeDuration : float.MaxValue;

        UpdateGroup(_menuObject, _menuGroup, _menuVisible, fadeRate);
        UpdateGroup(_restingObject, _restingGroup, _restingVisible, fadeRate);
    }

    public void ShowMenu(
        string message)
    {
        if (_menuText != null)
        {
            _menuText.text = message;
        }

        _menuVisible = true;
        _restingVisible = false;
    }

    public void ShowResting()
    {
        _restingVisible = true;
        _menuVisible = false;
    }

    public void HideAll()
    {
        _menuVisible = false;
        _restingVisible = false;
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
