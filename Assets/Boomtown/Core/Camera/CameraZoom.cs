using UnityEngine;

/// <summary>
/// Handles smooth mouse-wheel zoom along Main Camera local Z.
/// </summary>
[DisallowMultipleComponent]
public sealed class CameraZoom : MonoBehaviour
{
    [Header("Zoom")]
    [SerializeField, Min(0f)]
    private float _zoomSensitivity = 0.10f;

    [SerializeField, Min(0f)]
    private float _minimumZoomDistance = 5f;

    [SerializeField, Min(0f)]
    private float _maximumZoomDistance = 40f;

    [SerializeField, Min(0f)]
    private float _zoomSmoothing = 16f;

    private Transform _cameraTransform;
    private float _targetZoomDistance;

    public float NormalizedDistance =>
        Mathf.InverseLerp(
            _minimumZoomDistance,
            _maximumZoomDistance,
            _targetZoomDistance);

    public void Initialize(Transform cameraTransform)
    {
        _cameraTransform = cameraTransform;

        _targetZoomDistance = Mathf.Clamp(
            Mathf.Abs(_cameraTransform.localPosition.z),
            _minimumZoomDistance,
            _maximumZoomDistance);
    }

    public void Tick(BoomtownControls controls)
    {
        Vector2 scrollInput =
            controls.Camera.Zoom.ReadValue<Vector2>();

        _targetZoomDistance = Mathf.Clamp(
            _targetZoomDistance -
            (scrollInput.y * _zoomSensitivity),
            _minimumZoomDistance,
            _maximumZoomDistance);

        Vector3 targetPosition =
            new Vector3(0f, 0f, -_targetZoomDistance);

        float factor =
            1f - Mathf.Exp(
                -_zoomSmoothing * Time.deltaTime);

        _cameraTransform.localPosition =
            Vector3.Lerp(
                _cameraTransform.localPosition,
                targetPosition,
                factor);
    }
}
