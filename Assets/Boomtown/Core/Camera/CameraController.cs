using UnityEngine;

/// <summary>
/// Coordinates the complete Boomtown strategy camera.
/// Attach this component to CameraRig.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CameraMovement))]
[RequireComponent(typeof(CameraRotation))]
[RequireComponent(typeof(CameraZoom))]
[RequireComponent(typeof(CameraEdgeScroll))]
[RequireComponent(typeof(CameraBounds))]
[RequireComponent(typeof(CameraFollow))]
public sealed class CameraController : MonoBehaviour
{
    private BoomtownControls _controls;
    private CameraMovement _movement;
    private CameraRotation _rotation;
    private CameraZoom _zoom;
    private CameraEdgeScroll _edgeScroll;
    private CameraBounds _bounds;
    private CameraFollow _follow;

    private void Awake()
    {
        _controls = new BoomtownControls();

        _movement = GetComponent<CameraMovement>();
        _rotation = GetComponent<CameraRotation>();
        _zoom = GetComponent<CameraZoom>();
        _edgeScroll = GetComponent<CameraEdgeScroll>();
        _bounds = GetComponent<CameraBounds>();
        _follow = GetComponent<CameraFollow>();

        Transform yawTransform = transform.Find("Yaw");
        Transform pitchTransform = yawTransform != null
            ? yawTransform.Find("Pitch")
            : null;
        Camera childCamera = GetComponentInChildren<Camera>(true);

        if (yawTransform == null ||
            pitchTransform == null ||
            childCamera == null)
        {
            Debug.LogError(
                "CameraRig must contain Yaw/Pitch/Main Camera.",
                this);

            enabled = false;
            return;
        }

        _zoom.Initialize(childCamera.transform);
        _movement.Initialize(transform, yawTransform, _zoom);
        _rotation.Initialize(yawTransform, pitchTransform);
        _edgeScroll.Initialize(transform, yawTransform, _zoom);
        _bounds.Initialize(transform);
        _follow.Initialize(transform);
    }

    private void OnEnable()
    {
        _controls?.Camera.Enable();
    }

    private void OnDisable()
    {
        _controls?.Camera.Disable();
    }

    private void Update()
    {
        _follow.Tick(_controls);

        bool freeCamera = !_follow.IsFollowing;

        _movement.Tick(_controls, freeCamera);
        _edgeScroll.Tick(freeCamera);
        _rotation.Tick(_controls);
        _zoom.Tick(_controls);
    }

    private void LateUpdate()
    {
        _bounds.ClampPosition();
    }

    private void OnDestroy()
    {
        _controls?.Dispose();
    }
}
