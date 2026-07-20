using Boomtown.Gameplay.Prospecting;
using UnityEngine;

/// <summary>
/// Resolves the current waypoint command and drives queued activities
/// (e.g. panning for gold) identically for any waypoint-following character —
/// Bill (player-controlled) or an employee (AI-controlled). Movement itself
/// stays owned by the caller, since a player and an AI mover apply direction
/// to the transform differently; this class only decides what to do next.
/// </summary>
public sealed class WaypointActivityRunner
{
    private readonly WaypointPath _waypointPath;
    private readonly GoldPanningController _goldPanningController;
    private readonly string _ownerLabel;
    private readonly float _stoppingDistance;
    private readonly float _activityStoppingDistance;

    private bool _waitingForActivity;

    public WaypointActivityRunner(
        WaypointPath waypointPath,
        GoldPanningController goldPanningController,
        string ownerLabel,
        float stoppingDistance,
        float activityStoppingDistance)
    {
        _waypointPath = waypointPath;
        _goldPanningController = goldPanningController;
        _ownerLabel = ownerLabel;
        _stoppingDistance = stoppingDistance;
        _activityStoppingDistance = activityStoppingDistance;
    }

    public bool IsWaitingForActivity =>
        _waitingForActivity;

    /// <summary>
    /// Cancels an in-progress "waiting for activity" state, e.g. when the
    /// owner receives direct input and abandons its queued route.
    /// </summary>
    public void CancelWaiting()
    {
        _waitingForActivity = false;
    }

    /// <summary>
    /// Resolves the current waypoint command against the owner's position.
    /// Returns true with a normalized direction and distance when the owner
    /// should move this frame. Returns false when idle, waiting on an
    /// activity, or when arrival at the current command was just handled
    /// (completed, or started as an activity) this frame.
    /// </summary>
    public bool TryGetMoveDirection(
        Vector3 currentPosition,
        out Vector3 direction,
        out float distance,
        out bool hasQueuedCommand)
    {
        direction = Vector3.zero;
        distance = 0f;
        hasQueuedCommand = false;

        if (_waitingForActivity)
        {
            return false;
        }

        if (!_waypointPath.TryGetCurrentCommand(out WaypointCommand command))
        {
            return false;
        }

        hasQueuedCommand = true;

        Vector3 offset = command.Position - currentPosition;
        offset.y = 0f;

        float rawDistance = offset.magnitude;

        float stoppingDistance = command.IsActivity
            ? _activityStoppingDistance
            : _stoppingDistance;

        if (rawDistance <= stoppingDistance)
        {
            if (command.IsActivity)
            {
                StartActivity(command);
            }
            else
            {
                _waypointPath.CompleteCurrent();
            }

            return false;
        }

        direction = offset / rawDistance;
        distance = rawDistance;
        return true;
    }

    /// <summary>
    /// Changes the final queued waypoint into a panning activity, validating
    /// that one is queued and that it lands on a legal panning spot.
    /// </summary>
    public bool TryMarkLastWaypointAsPanning()
    {
        if (!_waypointPath.TryGetLastCommand(out WaypointCommand lastCommand))
        {
            Debug.Log(
                $"[Boomtown] Queue at least one waypoint for {_ownerLabel} " +
                "before pressing Shift + Space.");

            return false;
        }

        if (_goldPanningController == null ||
            !_goldPanningController.CanPanAt(lastCommand.Position))
        {
            Debug.Log(
                $"[Boomtown] {_ownerLabel}'s final waypoint is not a valid " +
                "gold-panning spot. Move it closer to a valid river bank.");

            return false;
        }

        return _waypointPath.MarkLastCommandAsActivity(
            WaypointCommandType.PanForGold);
    }

    private void StartActivity(WaypointCommand command)
    {
        if (_waitingForActivity)
        {
            return;
        }

        _waitingForActivity = true;
        bool started = false;

        if (command.CommandType == WaypointCommandType.PanForGold &&
            _goldPanningController != null)
        {
            started = _goldPanningController.TryStartPanning(CompleteActivity);
        }

        if (!started)
        {
            Debug.LogWarning(
                $"{_ownerLabel} could not perform queued activity " +
                $"{command.CommandType} at this location.");

            CompleteActivity();
        }
    }

    private void CompleteActivity()
    {
        _waypointPath.CompleteCurrent();
        _waitingForActivity = false;
    }
}
