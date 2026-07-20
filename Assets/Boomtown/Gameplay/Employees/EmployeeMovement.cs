using Boomtown.Gameplay.Prospecting;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
[RequireComponent(typeof(WaypointPath))]
public sealed class EmployeeMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField, Min(0f)] private float _movementSpeed = 3.5f;
    [SerializeField, Min(0f)] private float _acceleration = 12f;
    [SerializeField, Min(0f)] private float _deceleration = 16f;
    [SerializeField, Min(0f)] private float _rotationSpeed = 10f;
    [SerializeField, Min(0f)] private float _stoppingDistance = 0.15f;
    [SerializeField, Min(0.1f)] private float _activityStoppingDistance = 0.75f;

    [Header("Grounding")]
    [SerializeField] private float _groundOffset = 0f;
    [SerializeField, Min(0f)] private float _groundFollowSpeed = 20f;

    private WaypointPath _waypointPath;
    private GoldPanningController _goldPanningController;
    private NavMeshAgent _navMeshAgent;
    private WaypointActivityRunner _activityRunner;
    private float _currentSpeed;

    private void Awake()
    {
        _waypointPath = GetComponent<WaypointPath>();
        _goldPanningController = GetComponent<GoldPanningController>();
        _navMeshAgent = GetComponent<NavMeshAgent>();

        _activityRunner = new WaypointActivityRunner(
            _waypointPath,
            _goldPanningController,
            name,
            _stoppingDistance,
            _activityStoppingDistance);

        if (_navMeshAgent != null)
        {
            _navMeshAgent.updatePosition = false;
            _navMeshAgent.updateRotation = false;
            _navMeshAgent.updateUpAxis = false;
        }
    }

    private void Start()
    {
        SnapToTerrain();
        SyncNavMeshAgent();
    }

    private void Update()
    {
        if (_activityRunner.IsWaitingForActivity)
        {
            StopAndFollowTerrain();
            SyncNavMeshAgent();
            return;
        }

        bool isMoving = _activityRunner.TryGetMoveDirection(
            transform.position,
            out Vector3 direction,
            out float distance,
            out bool hasQueuedCommand);

        if (!hasQueuedCommand)
        {
            StopAndFollowTerrain();
            SyncNavMeshAgent();
            return;
        }

        if (!isMoving)
        {
            _currentSpeed = 0f;
            FollowTerrain();
            SyncNavMeshAgent();
            return;
        }

        MoveToward(direction, distance);
        SyncNavMeshAgent();
    }

    public void SetDestination(Vector3 destination, bool queueWaypoint)
    {
        Vector3 grounded = WaypointPath.GroundPoint(destination);

        if (queueWaypoint)
            _waypointPath.AddWaypoint(grounded);
        else
        {
            _activityRunner.CancelWaiting();
            _waypointPath.SetWaypoint(grounded);
        }
    }

    public bool MarkLastWaypointAsPanning()
    {
        return _activityRunner.TryMarkLastWaypointAsPanning();
    }

    private void MoveToward(Vector3 direction, float distance)
    {
        _currentSpeed = Mathf.MoveTowards(
            _currentSpeed,
            _movementSpeed,
            _acceleration * Time.deltaTime);

        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        float rotationFactor = 1f - Mathf.Exp(-_rotationSpeed * Time.deltaTime);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationFactor);

        float step = Mathf.Min(_currentSpeed * Time.deltaTime, distance);
        Vector3 nextPosition = transform.position + direction * step;
        nextPosition.y = GetCharacterHeight(nextPosition);
        transform.position = nextPosition;
    }

    private void StopAndFollowTerrain()
    {
        _currentSpeed = Mathf.MoveTowards(
            _currentSpeed,
            0f,
            _deceleration * Time.deltaTime);

        FollowTerrain();
    }

    private void FollowTerrain()
    {
        Vector3 position = transform.position;
        float targetY = GetCharacterHeight(position);

        position.y = Mathf.MoveTowards(
            position.y,
            targetY,
            _groundFollowSpeed * Time.deltaTime);

        transform.position = position;
    }

    private void SnapToTerrain()
    {
        Vector3 position = transform.position;
        position.y = GetCharacterHeight(position);
        transform.position = position;
    }

    private float GetCharacterHeight(Vector3 worldPosition)
    {
        Vector3 ground = WaypointPath.GroundPoint(worldPosition);
        return ground.y + _groundOffset;
    }

    private void SyncNavMeshAgent()
    {
        if (_navMeshAgent == null || !_navMeshAgent.enabled)
            return;

        _navMeshAgent.nextPosition = transform.position;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Mathf.Approximately(_groundOffset, 0f))
            _groundOffset = 0f;
    }
#endif
}
