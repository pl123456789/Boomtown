using UnityEngine;

/// <summary>
/// Controls camera centering and continuous follow for the active character.
/// C centers once, F toggles follow, and character cycling can request
/// an immediate center-and-follow operation.
/// </summary>
[DisallowMultipleComponent]
public sealed class CameraFollow : MonoBehaviour
{
    [Header("Active Target")]

    [SerializeField]
    private Transform _target;

    [Header("Focus")]

    [SerializeField]
    [Min(0f)]
    private float _focusSmoothing = 10f;

    [SerializeField]
    [Min(0f)]
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
            CenterOnTarget();
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

    /// <summary>
    /// Changes the active target without changing follow state.
    /// </summary>
    public void SetTarget(Transform target)
    {
        _target = target;

        if (_target == null)
        {
            IsFollowing = false;
            _isCentering = false;
        }
    }

    /// <summary>
    /// Changes the active target and smoothly centers the camera once.
    /// Existing continuous follow remains active.
    /// </summary>
    public void FocusTarget(Transform target)
    {
        SetTarget(target);

        if (_target != null)
        {
            _isCentering = true;
        }
    }

    /// <summary>
    /// Changes the target, centers the camera, and enables continuous follow.
    /// Used by Tab and Shift+Tab character cycling.
    /// </summary>
    public void FocusAndFollow(Transform target)
    {
        SetTarget(target);

        if (_target == null)
        {
            return;
        }

        IsFollowing = true;
        _isCentering = true;
    }

    /// <summary>
    /// Smoothly centers once on the current target.
    /// </summary>
    public void CenterOnTarget()
    {
        if (_target == null)
        {
            return;
        }

        if (!IsFollowing)
        {
            _isCentering = true;
        }
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
        _isCentering = true;
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

        Vector2 difference =
            new Vector2(
                _rigTransform.position.x -
                _target.position.x,
                _rigTransform.position.z -
                _target.position.z);

        if (difference.sqrMagnitude >
            _completionDistance * _completionDistance)
        {
            return;
        }

        _rigTransform.position = targetPosition;
        _isCentering = false;
    }
}
