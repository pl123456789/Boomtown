using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Selects employees, commands Bill or selected employees, switches camera focus,
/// and cycles through Bill and all employees with Tab or Shift+Tab.
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

    [SerializeField]
    [Min(0f)]
    private float _maximumRaycastDistance = 1000f;

    private readonly List<Employee> _employees = new();

    private Employee _selectedEmployee;
    private int _cycleIndex;

    private void Awake()
    {
        ResolveReferences();
        RefreshEmployeeList();

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

        SelectBill(false);
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard != null &&
            keyboard.tabKey.wasPressedThisFrame)
        {
            bool cycleBackward =
                keyboard.leftShiftKey.isPressed ||
                keyboard.rightShiftKey.isPressed;

            CycleCharacter(cycleBackward);
            return;
        }

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
                keyboard != null &&
                (keyboard.leftShiftKey.isPressed ||
                 keyboard.rightShiftKey.isPressed);

            CommandAt(
                pointer,
                queueWaypoint);
        }
    }

    /// <summary>
    /// Rebuilds the list used for Tab cycling.
    /// Call this after employees are hired or removed.
    /// </summary>
    public void RefreshEmployeeList()
    {
        _employees.Clear();

        Employee[] employees =
            FindObjectsByType<Employee>(
                FindObjectsSortMode.InstanceID);

        _employees.AddRange(employees);

        int maximumIndex = _employees.Count;

        if (_cycleIndex > maximumIndex)
        {
            _cycleIndex = 0;
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

    private void CycleCharacter(bool cycleBackward)
    {
        RefreshEmployeeList();

        int characterCount =
            _employees.Count + 1;

        if (characterCount <= 0)
        {
            return;
        }

        int direction =
            cycleBackward ? -1 : 1;

        _cycleIndex =
            (_cycleIndex + direction + characterCount) %
            characterCount;

        if (_cycleIndex == 0)
        {
            SelectBill(true);
            return;
        }

        Employee employee =
            _employees[_cycleIndex - 1];

        SelectEmployee(
            employee,
            true);
    }

    private void SelectAt(Vector2 screenPosition)
    {
        Ray ray =
            _worldCamera.ScreenPointToRay(
                screenPosition);

        RaycastHit[] hits =
            Physics.RaycastAll(
                ray,
                _maximumRaycastDistance,
                _employeeLayerMask,
                QueryTriggerInteraction.Ignore);

        Array.Sort(
            hits,
            (left, right) =>
                left.distance.CompareTo(
                    right.distance));

        foreach (RaycastHit hit in hits)
        {
            Employee employee =
                hit.collider
                    .GetComponentInParent<Employee>();

            if (employee != null)
            {
                SelectEmployee(
                    employee,
                    false);

                return;
            }

            QuickPlayerController bill =
                hit.collider
                    .GetComponentInParent<QuickPlayerController>();

            if (bill != null)
            {
                SelectBill(false);
                return;
            }
        }

        SelectBill(false);
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

    private void SelectEmployee(
        Employee employee,
        bool followImmediately)
    {
        ClearEmployeeSelection();

        _selectedEmployee = employee;
        _selectedEmployee.SetSelected(true);

        _cycleIndex =
            _employees.IndexOf(employee) + 1;

        if (followImmediately)
        {
            _cameraFollow.FocusAndFollow(
                _selectedEmployee.transform);
        }
        else
        {
            _cameraFollow.FocusTarget(
                _selectedEmployee.transform);
        }
    }

    private void SelectBill(bool followImmediately)
    {
        ClearEmployeeSelection();

        _cycleIndex = 0;

        if (followImmediately)
        {
            _cameraFollow.FocusAndFollow(
                _billController.transform);
        }
        else
        {
            _cameraFollow.FocusTarget(
                _billController.transform);
        }
    }

    private void ClearEmployeeSelection()
    {
        if (_selectedEmployee == null)
        {
            return;
        }

        _selectedEmployee.SetSelected(false);
        _selectedEmployee = null;
    }

    private static bool IsPointerOverUi()
    {
        return EventSystem.current != null &&
               EventSystem.current
                   .IsPointerOverGameObject();
    }
}
