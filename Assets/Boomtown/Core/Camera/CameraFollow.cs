using UnityEngine;

/// <summary>
/// C centers once. F centers and continuously follows the active target.
/// Press F again to release.
/// </summary>
[DisallowMultipleComponent]
public sealed class CameraFollow : MonoBehaviour
{
    [Header("Active Target")]
    [SerializeField]
    private Transform _target;

    [Header("Focus")]
    [SerializeField, Min(0f)]
    private float _focusSmoothing = 10f;

    [SerializeField, Min(0f)]
    private float _completionDistance = 0.05f;

    private Transform _rigTransform;
    private bool _isCentering;

    public Transform Target => _target;
    public bool IsFollowing { get; private set; }

    public void Initialize(Transform rigTransform)
    {
        _rigTransform = rigTransform;
    }

    public void Tick(BoomtownControls controls)
    {
        if (controls.Camera.Center.WasPressedThisFrame())
        {
            BeginCenter();
        }

        if (controls.Camera.Follow.WasPressedThisFrame())
        {
            ToggleFollow();
        }

        if (_target == null)
        {
            IsFollowing = false;
            _isCentering = false;
            return;
        }

        if (IsFollowing || _isCentering)
        {
            MoveTowardTarget();
        }
    }

    public void SetTarget(Transform target)
    {
        _target = target;

        if (_target == null)
        {
            IsFollowing = false;
            _isCentering = false;
        }
    }

    private void BeginCenter()
    {
        if (_target == null)
        {
            return;
        }

        IsFollowing = false;
        _isCentering = true;
    }

    private void ToggleFollow()
    {
        if (_target == null)
        {
            return;
        }

        if (IsFollowing)
        {
            IsFollowing = false;
            _isCentering = false;
            return;
        }

        IsFollowing = true;
        _isCentering = false;
    }

    private void MoveTowardTarget()
    {
        Vector3 targetPosition =
            new Vector3(
                _target.position.x,
                _rigTransform.position.y,
                _target.position.z);

        float factor =
            1f - Mathf.Exp(
                -_focusSmoothing * Time.deltaTime);

        _rigTransform.position =
            Vector3.Lerp(
                _rigTransform.position,
                targetPosition,
                factor);

        if (IsFollowing)
        {
            return;
        }

        Vector2 difference =
            new Vector2(
                _rigTransform.position.x -
                _target.position.x,
                _rigTransform.position.z -
                _target.position.z);

        if (difference.sqrMagnitude <=
            _completionDistance *
            _completionDistance)
        {
            _rigTransform.position =
                targetPosition;

            _isCentering = false;
        }
    }
}
