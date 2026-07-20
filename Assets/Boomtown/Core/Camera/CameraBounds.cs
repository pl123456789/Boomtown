using UnityEngine;

/// <summary>
/// Restricts CameraRig to configurable world-space X/Z limits.
/// </summary>
[DisallowMultipleComponent]
public sealed class CameraBounds : MonoBehaviour
{
    [Header("World Bounds")]
    [SerializeField]
    private bool _boundsEnabled = true;

    [SerializeField]
    private Vector2 _minimumPosition =
        new Vector2(-250f, -250f);

    [SerializeField]
    private Vector2 _maximumPosition =
        new Vector2(250f, 250f);

    private Transform _rigTransform;

    public void Initialize(Transform rigTransform)
    {
        _rigTransform = rigTransform;
    }

    public void ClampPosition()
    {
        if (!_boundsEnabled ||
            _rigTransform == null)
        {
            return;
        }

        Vector3 position =
            _rigTransform.position;

        position.x = Mathf.Clamp(
            position.x,
            _minimumPosition.x,
            _maximumPosition.x);

        position.z = Mathf.Clamp(
            position.z,
            _minimumPosition.y,
            _maximumPosition.y);

        _rigTransform.position = position;
    }

    private void OnValidate()
    {
        _maximumPosition.x = Mathf.Max(
            _maximumPosition.x,
            _minimumPosition.x);

        _maximumPosition.y = Mathf.Max(
            _maximumPosition.y,
            _minimumPosition.y);
    }
}
