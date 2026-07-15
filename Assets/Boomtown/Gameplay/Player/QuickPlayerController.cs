using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controls William "Bill" Parsons with camera-relative WASD
/// and optional mouse-commanded waypoint travel.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(WaypointPath))]
public sealed class QuickPlayerController : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField]
    private string _characterName =
        "William Parsons";

    [Header("References")]
    [SerializeField]
    private Transform _cameraYawTransform;

    [Header("Movement")]
    [SerializeField, Min(0f)]
    private float _movementSpeed = 5f;

    [SerializeField, Min(1f)]
    private float _runMultiplier = 1.75f;

    [SerializeField, Min(0f)]
    private float _acceleration = 30f;

    [SerializeField, Min(0f)]
    private float _deceleration = 40f;

    [SerializeField, Min(0f)]
    private float _rotationSpeed = 12f;

    [SerializeField, Min(0f)]
    private float _waypointStoppingDistance = 0.15f;

    [Header("Gravity")]
    [SerializeField]
    private float _gravity = -20f;

    private CharacterController _characterController;
    private WaypointPath _waypointPath;
    private Vector3 _horizontalVelocity;
    private float _verticalVelocity;

    public string CharacterName => _characterName;

    private void Awake()
    {
        _characterController =
            GetComponent<CharacterController>();

        _waypointPath =
            GetComponent<WaypointPath>();

        if (_cameraYawTransform == null)
        {
            GameObject yawObject =
                GameObject.Find("Yaw");

            if (yawObject != null)
            {
                _cameraYawTransform =
                    yawObject.transform;
            }
        }

        if (_cameraYawTransform == null)
        {
            Debug.LogError(
                "Bill needs CameraRig/Yaw assigned.",
                this);

            enabled = false;
        }
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return;
        }

        Vector2 directInput =
            ReadMovementInput(keyboard);

        Vector3 movementDirection;

        if (directInput.sqrMagnitude > 0f)
        {
            _waypointPath.Clear();

            movementDirection =
                CalculateCameraRelativeDirection(
                    directInput);
        }
        else
        {
            movementDirection =
                CalculateWaypointDirection();
        }

        float speed =
            keyboard.leftShiftKey.isPressed ||
            keyboard.rightShiftKey.isPressed
                ? _movementSpeed * _runMultiplier
                : _movementSpeed;

        UpdateHorizontalVelocity(
            movementDirection,
            speed);

        if (movementDirection.sqrMagnitude > 0f)
        {
            RotateToward(movementDirection);
        }

        UpdateGravity();

        Vector3 velocity =
            _horizontalVelocity +
            (Vector3.up * _verticalVelocity);

        _characterController.Move(
            velocity * Time.deltaTime);
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

    private Vector3 CalculateWaypointDirection()
    {
        if (!_waypointPath.TryGetCurrent(
            out Vector3 waypoint))
        {
            return Vector3.zero;
        }

        Vector3 offset =
            waypoint -
            transform.position;

        offset.y = 0f;

        if (offset.magnitude <=
            _waypointStoppingDistance)
        {
            _waypointPath.CompleteCurrent();
            return Vector3.zero;
        }

        return offset.normalized;
    }

    private Vector3 CalculateCameraRelativeDirection(
        Vector2 input)
    {
        Vector3 forward =
            _cameraYawTransform.forward;

        Vector3 right =
            _cameraYawTransform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        return Vector3.ClampMagnitude(
            (right * input.x) +
            (forward * input.y),
            1f);
    }

    private void UpdateHorizontalVelocity(
        Vector3 movementDirection,
        float speed)
    {
        Vector3 targetVelocity =
            movementDirection * speed;

        float movementRate =
            movementDirection.sqrMagnitude > 0f
                ? _acceleration
                : _deceleration;

        _horizontalVelocity =
            Vector3.MoveTowards(
                _horizontalVelocity,
                targetVelocity,
                movementRate *
                Time.deltaTime);
    }

    private static Vector2 ReadMovementInput(
        Keyboard keyboard)
    {
        Vector2 input = Vector2.zero;

        if (keyboard.aKey.isPressed)
        {
            input.x -= 1f;
        }

        if (keyboard.dKey.isPressed)
        {
            input.x += 1f;
        }

        if (keyboard.sKey.isPressed)
        {
            input.y -= 1f;
        }

        if (keyboard.wKey.isPressed)
        {
            input.y += 1f;
        }

        return Vector2.ClampMagnitude(
            input,
            1f);
    }

    private void RotateToward(
        Vector3 movementDirection)
    {
        Quaternion targetRotation =
            Quaternion.LookRotation(
                movementDirection,
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
    }

    private void UpdateGravity()
    {
        if (_characterController.isGrounded &&
            _verticalVelocity < 0f)
        {
            _verticalVelocity = 0f;
        }

        _verticalVelocity +=
            _gravity *
            Time.deltaTime;
    }
}
