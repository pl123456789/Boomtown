using Boomtown.Gameplay.Prospecting;
using UnityEngine;

/// <summary>
/// Moves an employee through terrain-grounded move and activity commands.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(WaypointPath))]
public sealed class EmployeeMovement : MonoBehaviour
{
    [Header("Movement")]

    [SerializeField, Min(0f)]
    private float _movementSpeed = 3.5f;

    [SerializeField, Min(0f)]
    private float _acceleration = 12f;

    [SerializeField, Min(0f)]
    private float _deceleration = 16f;

    [SerializeField, Min(0f)]
    private float _rotationSpeed = 10f;

    [SerializeField, Min(0f)]
    private float _stoppingDistance = 0.15f;

    [SerializeField, Min(0.1f)]
    private float _activityStoppingDistance = 0.75f;

    [Header("Grounding")]

    [SerializeField, Min(0f)]
    private float _groundOffset = 1f;

    [SerializeField, Min(0f)]
    private float _groundFollowSpeed = 20f;

    private WaypointPath _waypointPath;
    private GoldPanningController _goldPanningController;

    private float _currentSpeed;
    private bool _waitingForActivity;

    private void Awake()
    {
        _waypointPath =
            GetComponent<WaypointPath>();

        _goldPanningController =
            GetComponent<GoldPanningController>();
    }

    private void Start()
    {
        SnapToTerrain();
    }

    private void Update()
    {
        if (_waitingForActivity)
        {
            StopAndFollowTerrain();
            return;
        }

        if (!_waypointPath.TryGetCurrentCommand(
                out WaypointCommand command))
        {
            StopAndFollowTerrain();
            return;
        }

        Vector3 offset =
            command.Position -
            transform.position;

        offset.y = 0f;

        float distance =
            offset.magnitude;

        float stoppingDistance =
            command.IsActivity
                ? _activityStoppingDistance
                : _stoppingDistance;

        if (distance <= stoppingDistance)
        {
            if (command.IsActivity)
            {
                StartCurrentActivity(command);
            }
            else
            {
                _waypointPath.CompleteCurrent();
            }

            _currentSpeed = 0f;
            FollowTerrain();
            return;
        }

        MoveToward(
            offset / distance,
            distance);
    }

    public void SetDestination(
        Vector3 destination,
        bool queueWaypoint)
    {
        Vector3 grounded =
            WaypointPath.GroundPoint(
                destination);

        if (queueWaypoint)
        {
            _waypointPath.AddWaypoint(
                grounded);
        }
        else
        {
            _waitingForActivity = false;
            _waypointPath.SetWaypoint(
                grounded);
        }
    }

    public bool MarkLastWaypointAsPanning()
    {
        if (!_waypointPath.TryGetLastCommand(
                out WaypointCommand lastCommand))
        {
            Debug.Log(
                $"[Boomtown] Queue a waypoint for {name} before " +
                "pressing Shift + Space.",
                this);

            return false;
        }

        if (_goldPanningController == null ||
            !_goldPanningController.CanPanAt(
                lastCommand.Position))
        {
            Debug.Log(
                $"[Boomtown] {name}'s final waypoint is not a valid " +
                "gold-panning location.",
                this);

            return false;
        }

        return _waypointPath.MarkLastCommandAsActivity(
            WaypointCommandType.PanForGold);
    }

    private void StartCurrentActivity(
        WaypointCommand command)
    {
        if (_waitingForActivity)
        {
            return;
        }

        _waitingForActivity = true;
        _currentSpeed = 0f;

        bool started = false;

        if (command.CommandType ==
            WaypointCommandType.PanForGold &&
            _goldPanningController != null)
        {
            started =
                _goldPanningController.TryStartPanning(
                    CompleteCurrentActivity);
        }

        if (!started)
        {
            Debug.LogWarning(
                $"{name} could not perform queued activity " +
                $"{command.CommandType}.",
                this);

            CompleteCurrentActivity();
        }
    }

    private void CompleteCurrentActivity()
    {
        _waypointPath.CompleteCurrent();
        _waitingForActivity = false;
    }

    private void MoveToward(
        Vector3 direction,
        float distance)
    {
        _currentSpeed =
            Mathf.MoveTowards(
                _currentSpeed,
                _movementSpeed,
                _acceleration *
                Time.deltaTime);

        Quaternion targetRotation =
            Quaternion.LookRotation(
                direction,
                Vector3.up);

        float rotationFactor =
            1f -
            Mathf.Exp(
                -_rotationSpeed *
                Time.deltaTime);

        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationFactor);

        float step =
            Mathf.Min(
                _currentSpeed *
                Time.deltaTime,
                distance);

        Vector3 nextPosition =
            transform.position +
            direction * step;

        nextPosition.y =
            GetCharacterHeight(
                nextPosition);

        transform.position =
            nextPosition;
    }

    private void StopAndFollowTerrain()
    {
        _currentSpeed =
            Mathf.MoveTowards(
                _currentSpeed,
                0f,
                _deceleration *
                Time.deltaTime);

        FollowTerrain();
    }

    private void FollowTerrain()
    {
        Vector3 position =
            transform.position;

        float targetY =
            GetCharacterHeight(position);

        position.y =
            Mathf.MoveTowards(
                position.y,
                targetY,
                _groundFollowSpeed *
                Time.deltaTime);

        transform.position =
            position;
    }

    private void SnapToTerrain()
    {
        Vector3 position =
            transform.position;

        position.y =
            GetCharacterHeight(position);

        transform.position =
            position;
    }

    private float GetCharacterHeight(
        Vector3 worldPosition)
    {
        Vector3 ground =
            WaypointPath.GroundPoint(
                worldPosition);

        return ground.y +
               _groundOffset;
    }
}
