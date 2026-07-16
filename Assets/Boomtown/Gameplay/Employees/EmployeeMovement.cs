using UnityEngine;

/// <summary>
/// Moves an employee through a queue of RTS waypoints while keeping the
/// employee aligned with the active Unity terrain.
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

    [Header("Grounding")]
    [Tooltip("Height of the employee pivot above the terrain surface. " +
             "A standard Unity capsule with height 2 uses 1.")]
    [SerializeField, Min(0f)]
    private float _groundOffset = 1f;

    [Tooltip("How quickly the employee follows changes in terrain height.")]
    [SerializeField, Min(0f)]
    private float _groundFollowSpeed = 20f;

    private WaypointPath _waypointPath;
    private float _currentSpeed;

    private void Awake()
    {
        _waypointPath =
            GetComponent<WaypointPath>();
    }

    private void Start()
    {
        SnapToTerrain();
    }

    private void Update()
    {
        if (!_waypointPath.TryGetCurrent(
                out Vector3 destination))
        {
            _currentSpeed =
                Mathf.MoveTowards(
                    _currentSpeed,
                    0f,
                    _deceleration *
                    Time.deltaTime);

            FollowTerrain();
            return;
        }

        Vector3 offset =
            destination -
            transform.position;

        offset.y = 0f;

        float distance =
            offset.magnitude;

        if (distance <= _stoppingDistance)
        {
            _waypointPath.CompleteCurrent();
            _currentSpeed = 0f;
            FollowTerrain();
            return;
        }

        Vector3 direction =
            offset / distance;

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
            1f - Mathf.Exp(
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
            GetTerrainHeight(nextPosition);

        transform.position =
            nextPosition;
    }

    public void SetDestination(
        Vector3 destination,
        bool queueWaypoint)
    {
        Vector3 groundedDestination =
            new Vector3(
                destination.x,
                GetTerrainHeight(destination),
                destination.z);

        if (queueWaypoint)
        {
            _waypointPath.AddWaypoint(
                groundedDestination);
        }
        else
        {
            _waypointPath.SetWaypoint(
                groundedDestination);
        }
    }

    private void FollowTerrain()
    {
        Vector3 position =
            transform.position;

        float targetY =
            GetTerrainHeight(position);

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
            GetTerrainHeight(position);

        transform.position =
            position;
    }

    private float GetTerrainHeight(
        Vector3 worldPosition)
    {
        Terrain terrain =
            Terrain.activeTerrain;

        if (terrain == null ||
            terrain.terrainData == null)
        {
            return transform.position.y;
        }

        float terrainY =
            terrain.SampleHeight(
                worldPosition) +
            terrain.transform.position.y;

        return terrainY +
               _groundOffset;
    }
}