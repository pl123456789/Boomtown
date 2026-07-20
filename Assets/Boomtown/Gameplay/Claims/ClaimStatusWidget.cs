using Boomtown.Gameplay.Prospecting;
using TMPro;
using UnityEngine;

/// <summary>
/// Top-right HOI4-style card showing the local player's claim standing --
/// name, state, hired workers, time left before its standing degrades,
/// and gold recovered from it. Subscribes to ClaimManager's events rather
/// than polling every frame for state changes; only the countdown ticks
/// continuously, since that's genuinely time-based.
///
/// v1 shows a single card for one claim, matching the v1 design (one
/// claim per player). The card is laid out so a second/third card can be
/// stacked below it later without redesigning the container.
/// </summary>
public sealed class ClaimStatusWidget : MonoBehaviour
{
    [Header("Player")]

    [SerializeField]
    private MinerIdentity _playerIdentity;

    [Header("Objects")]

    [SerializeField]
    private GameObject _cardObject;

    [Header("Components")]

    [SerializeField]
    private TMP_Text _nameText;

    [SerializeField]
    private TMP_Text _stateText;

    [SerializeField]
    private TMP_Text _workersText;

    [SerializeField]
    private TMP_Text _countdownText;

    [SerializeField]
    private TMP_Text _goldText;

    [SerializeField, Min(0.01f)]
    private float _fadeDuration = 0.16f;

    private CanvasGroup _cardGroup;
    private ClaimManager _claimManager;
    private GoldInventory _playerGold;
    private ClaimData _trackedClaim;
    private bool _cardVisible;

    private float _goldRecoveredFromClaim;
    private float _lastKnownGold;

    private void Awake()
    {
        _cardGroup = GetOrAddCanvasGroup(_cardObject);
        SetGroupImmediate(_cardObject, _cardGroup, false);

        if (_playerIdentity == null)
        {
            GameObject bill = GameObject.Find("Bill");

            if (bill != null)
            {
                _playerIdentity = bill.GetComponent<MinerIdentity>();
                _playerGold = bill.GetComponent<GoldInventory>();
            }
        }
        else
        {
            _playerGold = _playerIdentity.GetComponent<GoldInventory>();
        }

        _claimManager = FindObjectOfType<ClaimManager>();

        if (_claimManager != null)
        {
            _claimManager.OnClaimRegistered += HandleClaimChanged;
            _claimManager.OnClaimStatusChanged += HandleClaimChanged;
            _claimManager.OnClaimWorked += HandleClaimWorked;
            _claimManager.OnClaimOwnershipChanged += HandleOwnershipChanged;
        }
    }

    private void OnDestroy()
    {
        if (_claimManager == null)
        {
            return;
        }

        _claimManager.OnClaimRegistered -= HandleClaimChanged;
        _claimManager.OnClaimStatusChanged -= HandleClaimChanged;
        _claimManager.OnClaimWorked -= HandleClaimWorked;
        _claimManager.OnClaimOwnershipChanged -= HandleOwnershipChanged;
    }

    private void Start()
    {
        RefreshTrackedClaim();
    }

    private void Update()
    {
        float fadeRate =
            _fadeDuration > 0f ? 1f / _fadeDuration : float.MaxValue;

        UpdateGroup(_cardObject, _cardGroup, _cardVisible, fadeRate);

        if (_trackedClaim != null)
        {
            RefreshDisplay();
        }
    }

    /// <summary>
    /// Lets the card be clicked to re-centre the camera on the claim.
    /// Wired up as a UI Button's onClick target.
    /// </summary>
    public void OnCardClicked()
    {
        if (_trackedClaim == null)
        {
            return;
        }

        GameObject rig = GameObject.Find("CameraRig");

        if (rig == null)
        {
            return;
        }

        Vector3 target = _trackedClaim.Midpoint;

        rig.transform.position = new Vector3(
            target.x,
            rig.transform.position.y,
            target.z);
    }

    private void HandleClaimChanged(
        ClaimData claim)
    {
        RefreshTrackedClaim();
    }

    private void HandleOwnershipChanged(
        ClaimData claim,
        MinerIdentity previousOwner,
        MinerIdentity newOwner)
    {
        RefreshTrackedClaim();
    }

    private void HandleClaimWorked(
        ClaimData claim,
        MinerIdentity worker)
    {
        if (claim != _trackedClaim ||
            worker != _playerIdentity ||
            _playerGold == null)
        {
            return;
        }

        float delta = _playerGold.TotalGoldOunces - _lastKnownGold;

        if (delta > 0f)
        {
            _goldRecoveredFromClaim += delta;
        }

        _lastKnownGold = _playerGold.TotalGoldOunces;
        RefreshDisplay();
    }

    private void RefreshTrackedClaim()
    {
        if (_claimManager == null || _playerIdentity == null)
        {
            _cardVisible = false;
            return;
        }

        ClaimData found = null;

        foreach (ClaimData claim in _claimManager.Claims)
        {
            if (claim != null &&
                claim.State != ClaimState.Unregistered &&
                claim.IsOwnerOrWorker(_playerIdentity))
            {
                found = claim;
                break;
            }
        }

        if (found != _trackedClaim)
        {
            _trackedClaim = found;

            if (found != null)
            {
                _lastKnownGold =
                    _playerGold != null ? _playerGold.TotalGoldOunces : 0f;

                _goldRecoveredFromClaim = 0f;
            }
        }

        _cardVisible = _trackedClaim != null;
        RefreshDisplay();
    }

    private void RefreshDisplay()
    {
        if (_trackedClaim == null)
        {
            return;
        }

        if (_nameText != null)
        {
            _nameText.text = _trackedClaim.ClaimName;
        }

        if (_stateText != null)
        {
            _stateText.text = FormatState(_trackedClaim.State);
        }

        int workerCount = _trackedClaim.AuthorizedWorkers.Count;

        if (_workersText != null)
        {
            _workersText.text = workerCount == 0
                ? "No hired workers"
                : $"{workerCount} worker{(workerCount == 1 ? string.Empty : "s")}";
        }

        if (_countdownText != null)
        {
            _countdownText.text =
                _trackedClaim.State == ClaimState.Forfeited
                    ? "Forfeited"
                    : "Represent within: " +
                      FormatDuration(
                          _claimManager.GetSecondsUntilNextDegradation(
                              _trackedClaim));
        }

        if (_goldText != null)
        {
            _goldText.text =
                $"Recovered: {_goldRecoveredFromClaim:0.####} oz";
        }
    }

    private static string FormatState(
        ClaimState state)
    {
        return state switch
        {
            ClaimState.Active => "Active",
            ClaimState.RepresentationOverdue => "Overdue",
            ClaimState.AtRisk => "At Risk",
            ClaimState.Forfeited => "Forfeited",
            _ => state.ToString()
        };
    }

    private static string FormatDuration(
        float seconds)
    {
        seconds = Mathf.Max(0f, seconds);
        int minutes = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        return $"{minutes}:{secs:00}";
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
