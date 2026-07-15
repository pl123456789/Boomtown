using UnityEngine;

/// <summary>
/// Handles Left Alt mouse rotation using separate Yaw and Pitch transforms.
/// </summary>
[DisallowMultipleComponent]
public sealed class CameraRotation : MonoBehaviour
{
    [Header("Mouse Rotation")]
    [SerializeField, Min(0f)]
    private float _rotationSensitivity = 0.15f;

    [SerializeField]
    private float _minimumPitch = 20f;

    [SerializeField]
    private float _maximumPitch = 75f;

    private Transform _yawTransform;
    private Transform _pitchTransform;
    private float _yaw;
    private float _pitch;

    public void Initialize(
        Transform yawTransform,
        Transform pitchTransform)
    {
        _yawTransform = yawTransform;
        _pitchTransform = pitchTransform;
        _yaw = _yawTransform.localEulerAngles.y;
        _pitch = Mathf.Clamp(
            NormalizeAngle(_pitchTransform.localEulerAngles.x),
            _minimumPitch,
            _maximumPitch);
    }

    public void Tick(BoomtownControls controls)
    {
        if (!controls.Camera.RotateModifier.IsPressed())
        {
            return;
        }

        Vector2 mouseDelta =
            controls.Camera.MouseDelta.ReadValue<Vector2>();

        _yaw += mouseDelta.x * _rotationSensitivity;
        _pitch = Mathf.Clamp(
            _pitch - (mouseDelta.y * _rotationSensitivity),
            _minimumPitch,
            _maximumPitch);

        _yawTransform.localRotation =
            Quaternion.Euler(0f, _yaw, 0f);

        _pitchTransform.localRotation =
            Quaternion.Euler(_pitch, 0f, 0f);
    }

    private static float NormalizeAngle(float angle)
    {
        return angle > 180f
            ? angle - 360f
            : angle;
    }
}
