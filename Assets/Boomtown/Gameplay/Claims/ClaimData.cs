using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One staked/registered claim. Lives on its own GameObject in the world
/// (so it can render its own boundary), created and owned exclusively
/// through ClaimManager -- nothing else should construct or mutate one
/// directly.
///
/// Deliberately references the shared geology data rather than copying any
/// of it: the claim answers "who can legally mine here", the existing
/// BoomtownGeologyData answers "how much gold is here". Keeping those
/// separate means claims never go stale relative to the real deposit data.
/// </summary>
[DisallowMultipleComponent]
public sealed class ClaimData : MonoBehaviour
{
    [SerializeField]
    private string _claimName = "Unnamed Claim";

    [SerializeField]
    private ClaimState _state = ClaimState.Unregistered;

    [SerializeField]
    private Vector3[] _cornerPosts = new Vector3[4];

    [SerializeField]
    private float _feePaid;

    private Guid _claimId;
    private MinerIdentity _owner;
    private readonly List<MinerIdentity> _authorizedWorkers = new();

    private float _dateStakedTime;
    private float _lastRepresentedTime;

    public Guid ClaimId => _claimId;
    public string ClaimName => _claimName;
    public ClaimState State => _state;
    public Vector3[] CornerPosts => _cornerPosts;
    public float FeePaid => _feePaid;
    public MinerIdentity Owner => _owner;
    public IReadOnlyList<MinerIdentity> AuthorizedWorkers => _authorizedWorkers;
    public float DateStakedTime => _dateStakedTime;
    public float LastRepresentedTime => _lastRepresentedTime;

    /// <summary>
    /// Called once by ClaimManager right after the GameObject is created.
    /// Not meant to be called again -- use the manager's own methods
    /// (RegisterClaim, AuthorizeWorker, RecordRepresentation, SetState) to
    /// change an existing claim so events fire correctly.
    /// </summary>
    public void Initialize(
        Vector3[] cornerPosts,
        string claimName,
        MinerIdentity owner)
    {
        _claimId = Guid.NewGuid();
        _cornerPosts = cornerPosts;
        _claimName = string.IsNullOrEmpty(claimName)
            ? "Unnamed Claim"
            : claimName;
        _owner = owner;
        _dateStakedTime = Time.time;
        _lastRepresentedTime = Time.time;
        _state = ClaimState.Unregistered;
    }

    public void MarkRegistered(
        float feePaid)
    {
        _feePaid = feePaid;
        _state = ClaimState.Active;
        _lastRepresentedTime = Time.time;
    }

    public void SetState(
        ClaimState state)
    {
        _state = state;
    }

    public void SetOwner(
        MinerIdentity owner)
    {
        _owner = owner;
    }

    public void RecordRepresentation()
    {
        _lastRepresentedTime = Time.time;
    }

    public bool AddAuthorizedWorker(
        MinerIdentity worker)
    {
        if (worker == null ||
            worker == _owner ||
            _authorizedWorkers.Contains(worker))
        {
            return false;
        }

        _authorizedWorkers.Add(worker);
        return true;
    }

    public bool RemoveAuthorizedWorker(
        MinerIdentity worker)
    {
        return _authorizedWorkers.Remove(worker);
    }

    public bool IsOwnerOrWorker(
        MinerIdentity identity)
    {
        return identity != null &&
               (identity == _owner ||
                _authorizedWorkers.Contains(identity));
    }

    /// <summary>
    /// Point-in-quad test against the four corner posts (an oriented
    /// rectangle -- bank frontage x fixed depth -- not an arbitrary
    /// polygon). Height (Y) is ignored; claims are staked on the ground
    /// plane.
    /// </summary>
    public bool Contains(
        Vector3 worldPosition)
    {
        if (_cornerPosts == null || _cornerPosts.Length != 4)
        {
            return false;
        }

        Vector2 point = new(worldPosition.x, worldPosition.z);
        bool inside = false;

        for (int i = 0, j = 3; i < 4; j = i++)
        {
            Vector2 a = new(_cornerPosts[i].x, _cornerPosts[i].z);
            Vector2 b = new(_cornerPosts[j].x, _cornerPosts[j].z);

            bool crosses =
                (a.y > point.y) != (b.y > point.y) &&
                point.x <
                    (b.x - a.x) * (point.y - a.y) /
                    (b.y - a.y) +
                    a.x;

            if (crosses)
            {
                inside = !inside;
            }
        }

        return inside;
    }
}
