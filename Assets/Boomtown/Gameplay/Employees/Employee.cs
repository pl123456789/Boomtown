using UnityEngine;

/// <summary>
/// Represents Theodore "Ted" Parsons, the first generic employee.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(EmployeeMovement))]
[RequireComponent(typeof(EmployeeSelectionRing))]
public sealed class Employee : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField]
    private string _employeeName =
        "Theodore Parsons";

    [Header("Employment")]
    [SerializeField]
    private EmployeeProfession _profession =
        EmployeeProfession.Unskilled;

    [SerializeField, Min(0)]
    private int _professionExperience;

    private EmployeeMovement _movement;
    private EmployeeSelectionRing _selectionRing;

    public string EmployeeName => _employeeName;
    public EmployeeProfession Profession => _profession;
    public int ProfessionExperience => _professionExperience;
    public bool IsSelected { get; private set; }

    private void Awake()
    {
        _movement =
            GetComponent<EmployeeMovement>();

        _selectionRing =
            GetComponent<EmployeeSelectionRing>();

        SetSelected(false);
    }

    public void SetSelected(bool isSelected)
    {
        IsSelected = isSelected;

        if (_selectionRing != null)
        {
            _selectionRing.SetVisible(isSelected);
        }
    }

    public void MoveTo(
        Vector3 destination,
        bool queueWaypoint)
    {
        _movement.SetDestination(
            destination,
            queueWaypoint);
    }

    public void SetProfession(
        EmployeeProfession profession)
    {
        if (_profession == profession)
        {
            return;
        }

        _profession = profession;
        _professionExperience = 0;
    }

    public void AddProfessionExperience(int amount)
    {
        _professionExperience =
            Mathf.Max(
                0,
                _professionExperience + amount);
    }
}
