using UnityEngine;

/// <summary>
/// Represents Theodore "Ted" Parsons, the first generic employee.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(EmployeeMovement))]
[RequireComponent(typeof(EmployeeSelectionRing))]
[RequireComponent(typeof(EmployeeNeeds))]
[RequireComponent(typeof(StickFigureWalkAnimator))]
[RequireComponent(typeof(GrubInventory))]
[RequireComponent(typeof(MinerIdentity))]
[RequireComponent(typeof(FreeMinerCertificate))]
[RequireComponent(typeof(PlayerInteractionFocus))]
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

    [Header("Grub")]

    [SerializeField, Min(0f)]
    private float _hungerRestoredPerGrub = 40f;

    private EmployeeMovement _movement;
    private EmployeeSelectionRing _selectionRing;
    private EmployeeNeeds _needs;
    private GrubInventory _grub;
    private MinerIdentity _identity;
    private FreeMinerCertificate _certificate;

    public string EmployeeName => _employeeName;
    public EmployeeProfession Profession => _profession;
    public int ProfessionExperience => _professionExperience;
    public bool IsSelected { get; private set; }
    public EmployeeNeeds Needs => _needs;
    public GrubInventory Grub => _grub;
    public MinerIdentity Identity => _identity;
    public FreeMinerCertificate Certificate => _certificate;

    private void Awake()
    {
        _movement =
            GetComponent<EmployeeMovement>();

        _selectionRing =
            GetComponent<EmployeeSelectionRing>();

        _needs =
            GetComponent<EmployeeNeeds>();

        _grub =
            GetComponent<GrubInventory>();

        _identity =
            GetComponent<MinerIdentity>();

        _identity.SetDisplayName(_employeeName);

        _certificate =
            GetComponent<FreeMinerCertificate>();

        SetSelected(false);
    }

    /// <summary>
    /// Eats one unit of carried grub to restore hunger. Returns false if
    /// there's no grub on hand.
    /// </summary>
    public bool TryEatGrub()
    {
        if (!_grub.TryRemoveGrub(1))
        {
            return false;
        }

        _needs.EatGrub(_hungerRestoredPerGrub);
        return true;
    }

    public void SetSelected(
        bool isSelected)
    {
        IsSelected = isSelected;

        if (_selectionRing != null)
        {
            _selectionRing.SetVisible(
                isSelected);
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

    public bool MarkLastWaypointAsPanning()
    {
        return _movement != null &&
               _movement.MarkLastWaypointAsPanning();
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

    public void AddProfessionExperience(
        int amount)
    {
        _professionExperience =
            Mathf.Max(
                0,
                _professionExperience +
                amount);
    }
}
