using UnityEngine;

/// <summary>
/// Moves an employee through a queue of RTS waypoints.
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

    private WaypointPath _waypointPath;
    private float _currentSpeed;

    private void Awake()
    {
        _waypointPath =
            GetComponent<WaypointPath>();
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

        float factor =
            1f - Mathf.Exp(
                -_rotationSpeed *
                Time.deltaTime);

        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                factor);

        float step =
            Mathf.Min(
                _currentSpeed *
                Time.deltaTime,
                distance);

        transform.position +=
            direction * step;
    }

    public void SetDestination(
        Vector3 destination,
        bool queueWaypoint)
    {
        Vector3 groundedDestination =
            new Vector3(
                destination.x,
                transform.position.y,
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
}
