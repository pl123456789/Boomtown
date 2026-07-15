using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Selects Ted, commands Bill or Ted, and switches camera focus.
/// </summary>
[DisallowMultipleComponent]
public sealed class EmployeeSelectionManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Camera _worldCamera;

    [SerializeField]
    private CameraFollow _cameraFollow;

    [SerializeField]
    private QuickPlayerController _billController;

    [Header("Raycasts")]
    [SerializeField]
    private LayerMask _employeeLayerMask = ~0;

    [SerializeField]
    private LayerMask _groundLayerMask = ~0;

    [SerializeField, Min(0f)]
    private float _maximumRaycastDistance = 1000f;

    private Employee _selectedEmployee;

    private void Awake()
    {
        ResolveReferences();

        if (_worldCamera == null ||
            _cameraFollow == null ||
            _billController == null)
        {
            Debug.LogError(
                "EmployeeSelectionManager is missing required references.",
                this);

            enabled = false;
            return;
        }

        _cameraFollow.SetTarget(
            _billController.transform);
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;

        if (mouse == null ||
            IsPointerOverUi())
        {
            return;
        }

        Vector2 pointer =
            mouse.position.ReadValue();

        if (mouse.leftButton.wasPressedThisFrame)
        {
            SelectAt(pointer);
        }

        if (mouse.rightButton.wasPressedThisFrame)
        {
            bool queueWaypoint =
                Keyboard.current != null &&
                (Keyboard.current.leftShiftKey.isPressed ||
                 Keyboard.current.rightShiftKey.isPressed);

            CommandAt(
                pointer,
                queueWaypoint);
        }
    }

    private void ResolveReferences()
    {
        if (_worldCamera == null)
        {
            _worldCamera = Camera.main;
        }

        if (_cameraFollow == null)
        {
            _cameraFollow =
                FindFirstObjectByType<CameraFollow>();
        }

        if (_billController == null)
        {
            _billController =
                FindFirstObjectByType<QuickPlayerController>();
        }
    }

    private void SelectAt(Vector2 screenPosition)
    {
        Ray ray =
            _worldCamera.ScreenPointToRay(
                screenPosition);

        if (Physics.Raycast(
            ray,
            out RaycastHit hit,
            _maximumRaycastDistance,
            _employeeLayerMask,
            QueryTriggerInteraction.Ignore))
        {
            Employee employee =
                hit.collider
                    .GetComponentInParent<Employee>();

            if (employee != null)
            {
                SelectEmployee(employee);
                return;
            }
        }

        DeselectEmployee();
    }

    private void CommandAt(
        Vector2 screenPosition,
        bool queueWaypoint)
    {
        if (!TryGetGroundPoint(
            screenPosition,
            out Vector3 destination))
        {
            return;
        }

        if (_selectedEmployee != null)
        {
            _selectedEmployee.MoveTo(
                destination,
                queueWaypoint);

            return;
        }

        _billController.SetDestination(
            destination,
            queueWaypoint);
    }

    private bool TryGetGroundPoint(
        Vector2 screenPosition,
        out Vector3 destination)
    {
        Ray ray =
            _worldCamera.ScreenPointToRay(
                screenPosition);

        RaycastHit[] hits =
            Physics.RaycastAll(
                ray,
                _maximumRaycastDistance,
                _groundLayerMask,
                QueryTriggerInteraction.Ignore);

        Array.Sort(
            hits,
            (left, right) =>
                left.distance.CompareTo(
                    right.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider
                .GetComponentInParent<Employee>() != null)
            {
                continue;
            }

            if (hit.collider
                .GetComponentInParent<QuickPlayerController>() != null)
            {
                continue;
            }

            destination = hit.point;
            return true;
        }

        destination = default;
        return false;
    }

    private void SelectEmployee(Employee employee)
    {
        if (_selectedEmployee != null &&
            _selectedEmployee != employee)
        {
            _selectedEmployee.SetSelected(false);
        }

        _selectedEmployee = employee;
        _selectedEmployee.SetSelected(true);

        _cameraFollow.SetTarget(
            _selectedEmployee.transform);
    }

    private void DeselectEmployee()
    {
        if (_selectedEmployee != null)
        {
            _selectedEmployee.SetSelected(false);
            _selectedEmployee = null;
        }

        _cameraFollow.SetTarget(
            _billController.transform);
    }

    private static bool IsPointerOverUi()
    {
        return EventSystem.current != null &&
               EventSystem.current
                   .IsPointerOverGameObject();
    }
}
