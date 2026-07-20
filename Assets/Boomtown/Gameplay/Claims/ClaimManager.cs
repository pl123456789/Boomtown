using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The single authority on claim ownership and access. Nothing else --
/// not GoldPanningController, not the UI, not the staking controller --
/// should inspect a ClaimData's owner/state directly to decide whether an
/// action is allowed. They ask CheckAccess and get an AccessResult back.
/// That indirection is what lets claim jumping, government claims,
/// multiplayer ownership, etc. get added later without touching the
/// mining code at all.
///
/// A scene-resident singleton discovered the same way GeneralStoreUI and
/// PlayerNeedsUI are (FindObjectOfType), not a persistent DontDestroyOnLoad
/// singleton -- consistent with how the rest of the project's per-scene
/// systems are found.
/// </summary>
[DisallowMultipleComponent]
public sealed class ClaimManager : MonoBehaviour
{
    [Header("Representation Timing")]
    [Tooltip(
        "Real seconds a claim can go unworked before it's flagged " +
        "Overdue. Named after the historical 72-hour \"represent your " +
        "claim\" rule, but compressed to a testable session length until " +
        "the project has a real day/night calendar -- retune freely.")]
    [SerializeField, Min(1f)]
    private float _activeWindowSeconds = 900f;

    [Tooltip("Additional seconds past Overdue before a claim is At Risk.")]
    [SerializeField, Min(1f)]
    private float _overdueWindowSeconds = 300f;

    [Tooltip("Additional seconds past At Risk before a claim is Forfeited.")]
    [SerializeField, Min(1f)]
    private float _atRiskWindowSeconds = 300f;

    [Tooltip("How often the forfeiture clock is checked.")]
    [SerializeField, Min(0.1f)]
    private float _tickIntervalSeconds = 5f;

    private readonly List<ClaimData> _claims = new();
    private Transform _claimsContainer;
    private float _timeSinceLastTick;

    public event Action<ClaimData> OnClaimRegistered;
    public event Action<ClaimData> OnClaimStatusChanged;
    public event Action<ClaimData, MinerIdentity> OnClaimWorked;
    public event Action<ClaimData, MinerIdentity, MinerIdentity> OnClaimOwnershipChanged;

    public IReadOnlyList<ClaimData> Claims => _claims;

    private void Update()
    {
        _timeSinceLastTick += Time.deltaTime;

        if (_timeSinceLastTick < _tickIntervalSeconds)
        {
            return;
        }

        _timeSinceLastTick = 0f;
        TickForfeitureClock();
    }

    /// <summary>
    /// Creates a staked-but-unregistered claim. Not exclusive yet -- the
    /// ground stays open to anyone until it's paid for and registered at
    /// the Gold Commissioner's Office via TryRegisterClaim. No event fires
    /// here; the staking controller holds the returned reference directly.
    /// </summary>
    public ClaimData CreateUnregisteredClaim(
        Vector3[] cornerPosts,
        string claimName,
        MinerIdentity owner)
    {
        if (cornerPosts == null || cornerPosts.Length != 4 || owner == null)
        {
            Debug.LogError(
                "[Claim Manager] Cannot stake a claim without four " +
                "corner posts and an owner.");

            return null;
        }

        EnsureContainer();

        GameObject claimObject =
            new($"Claim (Unregistered) - {owner.DisplayName}");

        claimObject.transform.SetParent(_claimsContainer, true);

        ClaimData claim = claimObject.AddComponent<ClaimData>();
        claim.Initialize(cornerPosts, claimName, owner);

        _claims.Add(claim);
        return claim;
    }

    /// <summary>
    /// Pays for and activates a staked claim. Called by the Gold
    /// Commissioner's Office once the player accepts the fee.
    /// </summary>
    public bool TryRegisterClaim(
        ClaimData claim,
        float feePaid)
    {
        if (claim == null || claim.State != ClaimState.Unregistered)
        {
            return false;
        }

        claim.MarkRegistered(feePaid);
        claim.gameObject.name = $"Claim - {claim.ClaimName}";

        OnClaimRegistered?.Invoke(claim);
        OnClaimStatusChanged?.Invoke(claim);
        return true;
    }

    /// <summary>
    /// The single permission check every other system should go through.
    /// </summary>
    public AccessResult CheckAccess(
        Vector3 worldPosition,
        MinerIdentity requester)
    {
        ClaimData claim = FindClaimContaining(worldPosition);

        if (claim == null || claim.State == ClaimState.Unregistered)
        {
            return AccessResult.Unclaimed;
        }

        if (claim.State == ClaimState.Forfeited)
        {
            return AccessResult.Forfeited;
        }

        if (!claim.IsOwnerOrWorker(requester))
        {
            return AccessResult.OthersClaim;
        }

        FreeMinerCertificate certificate =
            requester != null
                ? requester.GetComponent<FreeMinerCertificate>()
                : null;

        if (certificate == null || !certificate.IsValid)
        {
            return AccessResult.CertificateExpired;
        }

        return requester == claim.Owner
            ? AccessResult.Owner
            : AccessResult.AuthorizedWorker;
    }

    /// <summary>
    /// Call whenever a pan actually succeeds inside an owned/authorized
    /// claim so its 72-hour clock resets. Does nothing (silently) if the
    /// position isn't inside an active claim -- callers don't need to
    /// pre-check, they can call this unconditionally after a successful
    /// pan and let the manager sort out whether it mattered.
    /// </summary>
    public void RecordRepresentation(
        Vector3 worldPosition,
        MinerIdentity worker)
    {
        ClaimData claim = FindClaimContaining(worldPosition);

        if (claim == null || !claim.IsOwnerOrWorker(worker))
        {
            return;
        }

        bool wasAtRisk =
            claim.State == ClaimState.RepresentationOverdue ||
            claim.State == ClaimState.AtRisk;

        claim.RecordRepresentation();

        if (wasAtRisk)
        {
            claim.SetState(ClaimState.Active);
            OnClaimStatusChanged?.Invoke(claim);
        }

        OnClaimWorked?.Invoke(claim, worker);
    }

    public bool AuthorizeWorker(
        ClaimData claim,
        MinerIdentity worker)
    {
        if (claim == null || !claim.AddAuthorizedWorker(worker))
        {
            return false;
        }

        OnClaimStatusChanged?.Invoke(claim);
        return true;
    }

    public bool RevokeWorker(
        ClaimData claim,
        MinerIdentity worker)
    {
        if (claim == null || !claim.RemoveAuthorizedWorker(worker))
        {
            return false;
        }

        OnClaimStatusChanged?.Invoke(claim);
        return true;
    }

    public void TransferOwnership(
        ClaimData claim,
        MinerIdentity newOwner)
    {
        if (claim == null || newOwner == null)
        {
            return;
        }

        MinerIdentity previousOwner = claim.Owner;
        claim.SetOwner(newOwner);

        OnClaimOwnershipChanged?.Invoke(claim, previousOwner, newOwner);
        OnClaimStatusChanged?.Invoke(claim);
    }

    /// <summary>
    /// Finds a claim this identity staked but hasn't registered yet, for
    /// the Gold Commissioner's Office to look up when the player walks in
    /// wanting to pay for and finalize whatever they just staked.
    /// </summary>
    public ClaimData FindUnregisteredClaimOwnedBy(
        MinerIdentity owner)
    {
        if (owner == null)
        {
            return null;
        }

        for (int i = 0; i < _claims.Count; i++)
        {
            ClaimData claim = _claims[i];

            if (claim != null &&
                claim.State == ClaimState.Unregistered &&
                claim.Owner == owner)
            {
                return claim;
            }
        }

        return null;
    }

    /// <summary>
    /// Seconds left in the claim's current standing before it degrades to
    /// the next forfeiture stage (Active -> Overdue -> At Risk ->
    /// Forfeited). Exposed so UI can show a countdown without duplicating
    /// the tuning constants above. Returns 0 once already Forfeited.
    /// </summary>
    public float GetSecondsUntilNextDegradation(
        ClaimData claim)
    {
        if (claim == null || claim.State == ClaimState.Forfeited)
        {
            return 0f;
        }

        float unworkedFor = Time.time - claim.LastRepresentedTime;

        float activeEnd = _activeWindowSeconds;
        float overdueEnd = activeEnd + _overdueWindowSeconds;
        float atRiskEnd = overdueEnd + _atRiskWindowSeconds;

        if (unworkedFor < activeEnd)
        {
            return activeEnd - unworkedFor;
        }

        if (unworkedFor < overdueEnd)
        {
            return overdueEnd - unworkedFor;
        }

        if (unworkedFor < atRiskEnd)
        {
            return atRiskEnd - unworkedFor;
        }

        return 0f;
    }

    public ClaimData FindClaimContaining(
        Vector3 worldPosition)
    {
        for (int i = 0; i < _claims.Count; i++)
        {
            ClaimData claim = _claims[i];

            if (claim != null && claim.Contains(worldPosition))
            {
                return claim;
            }
        }

        return null;
    }

    private void TickForfeitureClock()
    {
        for (int i = 0; i < _claims.Count; i++)
        {
            ClaimData claim = _claims[i];

            if (claim == null ||
                claim.State == ClaimState.Unregistered ||
                claim.State == ClaimState.Forfeited)
            {
                continue;
            }

            float unworkedFor = Time.time - claim.LastRepresentedTime;
            ClaimState nextState = ComputeState(unworkedFor);

            if (nextState != claim.State)
            {
                claim.SetState(nextState);
                OnClaimStatusChanged?.Invoke(claim);
            }
        }
    }

    private ClaimState ComputeState(
        float unworkedForSeconds)
    {
        if (unworkedForSeconds <
            _activeWindowSeconds)
        {
            return ClaimState.Active;
        }

        if (unworkedForSeconds <
            _activeWindowSeconds + _overdueWindowSeconds)
        {
            return ClaimState.RepresentationOverdue;
        }

        if (unworkedForSeconds <
            _activeWindowSeconds +
            _overdueWindowSeconds +
            _atRiskWindowSeconds)
        {
            return ClaimState.AtRisk;
        }

        return ClaimState.Forfeited;
    }

    private void EnsureContainer()
    {
        if (_claimsContainer != null)
        {
            return;
        }

        GameObject existing = GameObject.Find("Claims");

        _claimsContainer =
            existing != null
                ? existing.transform
                : new GameObject("Claims").transform;
    }
}
