using Boomtown.Gameplay.Prospecting;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controls William "Bill" Parsons with camera-relative WASD and queued
/// terrain-grounded commands.
///
/// Shift + Space changes the final queued waypoint into an interaction X.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(WaypointPath))]
[RequireComponent(typeof(EmployeeNeeds))]
[RequireComponent(typeof(StickFigureWalkAnimator))]
[RequireComponent(typeof(GrubInventory))]
[RequireComponent(typeof(PlayerNeedsMenu))]
[RequireComponent(typeof(MinerIdentity))]
[RequireComponent(typeof(FreeMinerCertificate))]
[RequireComponent(typeof(ClaimStakingController))]
[RequireComponent(typeof(PlayerInteractionFocus))]
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

    [SerializeField, Min(0.1f)]
    private float _activityStoppingDistance = 0.75f;

    [Header("Gravity")]

    [SerializeField]
    private float _gravity = -20f;

    [SerializeField, Min(0f)]
    private float _groundedDownwardSpeed = 2f;

    [Header("Grub")]

    [SerializeField, Min(0f)]
    private float _hungerRestoredPerGrub = 40f;

    private CharacterController _characterController;
    private WaypointPath _waypointPath;
    private GoldPanningController _goldPanningController;
    private EmployeeNeeds _needs;
    private GrubInventory _grub;
    private MinerIdentity _identity;
    private FreeMinerCertificate _certificate;
    private WaypointActivityRunner _activityRunner;

    private Vector3 _horizontalVelocity;
    private float _verticalVelocity;

    public string CharacterName =>
        _characterName;

    public EmployeeNeeds Needs =>
        _needs;

    public GrubInventory Grub =>
        _grub;

    public MinerIdentity Identity =>
        _identity;

    public FreeMinerCertificate Certificate =>
        _certificate;

    private void Awake()
    {
        _characterController =
            GetComponent<CharacterController>();

        _waypointPath =
            GetComponent<WaypointPath>();

        _goldPanningController =
            GetComponent<GoldPanningController>();

        _needs =
            GetComponent<EmployeeNeeds>();

        _grub =
            GetComponent<GrubInventory>();

        _identity =
            GetComponent<MinerIdentity>();

        _identity.SetDisplayName(_characterName);

        _certificate =
            GetComponent<FreeMinerCertificate>();

        _activityRunner =
            new WaypointActivityRunner(
                _waypointPath,
                _goldPanningController,
                _characterName,
                _waypointStoppingDistance,
                _activityStoppingDistance);

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
        Keyboard keyboard =
            Keyboard.current;

        if (keyboard == null)
        {
            return;
        }

        HandleQueuedInteractionInput(
            keyboard);

        Vector2 directInput =
            ReadMovementInput(
                keyboard);

        Vector3 movementDirection;

        if (directInput.sqrMagnitude > 0f)
        {
            _activityRunner.CancelWaiting();
            _waypointPath.Clear();

            movementDirection =
                CalculateCameraRelativeDirection(
                    directInput);
        }
        else if (_activityRunner.IsWaitingForActivity)
        {
            movementDirection =
                Vector3.zero;
        }
        else if (_activityRunner.TryGetMoveDirection(
                     transform.position,
                     out Vector3 commandDirection,
                     out _,
                     out _))
        {
            movementDirection =
                commandDirection;
        }
        else
        {
            movementDirection =
                Vector3.zero;
        }

        float speed =
            keyboard.leftShiftKey.isPressed ||
            keyboard.rightShiftKey.isPressed
                ? _movementSpeed *
                  _runMultiplier
                : _movementSpeed;

        UpdateHorizontalVelocity(
            movementDirection,
            speed);

        if (movementDirection.sqrMagnitude > 0f)
        {
            RotateToward(
                movementDirection);
        }

        UpdateGravity();

        Vector3 velocity =
            _horizontalVelocity +
            Vector3.up *
            _verticalVelocity;

        _characterController.Move(
            velocity *
            Time.deltaTime);
    }

    public void SetDestination(
        Vector3 destination,
        bool queueWaypoint)
    {
        Vector3 groundedDestination =
            WaypointPath.GroundPoint(
                destination);

        if (queueWaypoint)
        {
            _waypointPath.AddWaypoint(
                groundedDestination);
        }
        else
        {
            _activityRunner.CancelWaiting();

            _waypointPath.SetWaypoint(
                groundedDestination);
        }
    }

    private void HandleQueuedInteractionInput(
        Keyboard keyboard)
    {
        bool shiftPressed =
            keyboard.leftShiftKey.isPressed ||
            keyboard.rightShiftKey.isPressed;

        if (!shiftPressed ||
            !keyboard.spaceKey.wasPressedThisFrame)
        {
            return;
        }

        _activityRunner.TryMarkLastWaypointAsPanning();
    }

    public bool MarkLastWaypointAsPanning()
    {
        return _activityRunner.TryMarkLastWaypointAsPanning();
    }

    /// <summary>
    /// Eats one unit of carried grub to restore hunger. Returns false if
    /// there's no grub on hand.
    /// </summary>
    public bool TryEatGrub()
    {
        if (!_grub.TryRemoveGrub(1))
        {
            return false;
        }

        _needs.EatGrub(_hungerRestoredPerGrub);
        return true;
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
            right * input.x +
            forward * input.y,
            1f);
    }

    private void UpdateHorizontalVelocity(
        Vector3 movementDirection,
        float speed)
    {
        Vector3 targetVelocity =
            movementDirection *
            speed;

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
        Vector2 input =
            Vector2.zero;

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
            1f -
            Mathf.Exp(
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
        if (_characterController.isGrounded)
        {
            // Keep Bill gently pressed onto descending terrain instead of
            // resetting vertical velocity to zero and repeatedly losing contact.
            _verticalVelocity =
                -_groundedDownwardSpeed;

            return;
        }

        _verticalVelocity +=
            _gravity *
            Time.deltaTime;
    }
}
