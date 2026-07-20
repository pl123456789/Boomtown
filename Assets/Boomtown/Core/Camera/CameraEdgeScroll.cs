using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Moves the free camera when the pointer reaches a screen edge.
/// </summary>
[DisallowMultipleComponent]
public sealed class CameraEdgeScroll : MonoBehaviour
{
    [Header("Edge Scrolling")]
    [SerializeField]
    private bool _edgeScrollingEnabled = true;

    [SerializeField, Min(1f)]
    private float _borderSizePixels = 12f;

    [SerializeField, Min(0f)]
    private float _edgeScrollSpeed = 8f;

    [SerializeField, Min(0f)]
    private float _closeZoomSpeedMultiplier = 0.55f;

    [SerializeField, Min(0f)]
    private float _farZoomSpeedMultiplier = 1.65f;

    private Transform _rigTransform;
    private Transform _yawTransform;
    private CameraZoom _zoom;

    public void Initialize(
        Transform rigTransform,
        Transform yawTransform,
        CameraZoom zoom)
    {
        _rigTransform = rigTransform;
        _yawTransform = yawTransform;
        _zoom = zoom;
    }

    public void Tick(bool movementEnabled)
    {
        if (!_edgeScrollingEnabled ||
            !movementEnabled ||
            Mouse.current == null)
        {
            return;
        }

        Vector2 pointer =
            Mouse.current.position.ReadValue();

        Vector2 input = Vector2.zero;

        if (pointer.x <= _borderSizePixels)
        {
            input.x = -1f;
        }
        else if (pointer.x >=
                 Screen.width - _borderSizePixels)
        {
            input.x = 1f;
        }

        if (pointer.y <= _borderSizePixels)
        {
            input.y = -1f;
        }
        else if (pointer.y >=
                 Screen.height - _borderSizePixels)
        {
            input.y = 1f;
        }

        if (input == Vector2.zero)
        {
            return;
        }

        Vector3 forward = _yawTransform.forward;
        Vector3 right = _yawTransform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        float zoomMultiplier = Mathf.Lerp(
            _closeZoomSpeedMultiplier,
            _farZoomSpeedMultiplier,
            _zoom.NormalizedDistance);

        Vector3 direction =
            Vector3.ClampMagnitude(
                (right * input.x) +
                (forward * input.y),
                1f);

        _rigTransform.position +=
            direction *
            (_edgeScrollSpeed *
             zoomMultiplier *
             Time.deltaTime);
    }
}
