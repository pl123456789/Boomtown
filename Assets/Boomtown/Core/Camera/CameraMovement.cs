using UnityEngine;

/// <summary>
/// Handles middle-mouse camera panning.
/// WASD is reserved for Bill.
/// </summary>
[DisallowMultipleComponent]
public sealed class CameraMovement : MonoBehaviour
{
    [Header("Mouse Pan")]
    [SerializeField, Min(0f)]
    private float _mousePanSensitivity = 0.03f;

    [Header("Zoom-aware Pan Speed")]
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

    public void Tick(
        BoomtownControls controls,
        bool movementEnabled)
    {
        if (!movementEnabled ||
            !controls.Camera.PanModifier.IsPressed() ||
            controls.Camera.RotateModifier.IsPressed())
        {
            return;
        }

        Vector2 mouseDelta =
            controls.Camera.MouseDelta.ReadValue<Vector2>();

        Vector3 forward =
            FlattenAndNormalize(_yawTransform.forward);

        Vector3 right =
            FlattenAndNormalize(_yawTransform.right);

        float zoomMultiplier = Mathf.Lerp(
            _closeZoomSpeedMultiplier,
            _farZoomSpeedMultiplier,
            _zoom.NormalizedDistance);

        Vector3 movement =
            ((-right * mouseDelta.x) +
             (-forward * mouseDelta.y)) *
            (_mousePanSensitivity * zoomMultiplier);

        _rigTransform.position += movement;
    }

    private static Vector3 FlattenAndNormalize(
        Vector3 value)
    {
        value.y = 0f;

        return value.sqrMagnitude > 0f
            ? value.normalized
            : Vector3.zero;
    }
}
