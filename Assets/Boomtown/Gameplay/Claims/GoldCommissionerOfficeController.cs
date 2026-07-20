using Boomtown.Gameplay.Prospecting;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// The Gold Commissioner's Office: walk into its trigger, press the trade
/// key for a panel with two period-accurate transactions -- pay for a
/// Free Miner's Certificate (required to legally pan anywhere, including
/// your own claim), and register whatever claim you've staked but not
/// yet paid for. Same trigger/key interaction pattern as
/// GeneralStoreController.
/// </summary>
[DisallowMultipleComponent]
public sealed class GoldCommissionerOfficeController : MonoBehaviour
{
    [Header("Fees (gold ounces)")]

    [SerializeField, Min(0f)]
    private float _certificateFee = 0.05f;

    [SerializeField, Min(0f)]
    private float _claimRegistrationFee = 0.02f;

    [Header("Interaction")]

    [SerializeField]
    private Key _tradeKey = Key.T;

    [SerializeField]
    private Key _certificateKey = Key.C;

    [SerializeField]
    private Key _registerClaimKey = Key.F;

    [Header("UI")]

    [SerializeField]
    private GoldCommissionerOfficeUI _ui;

    private ClaimManager _claimManager;
    private GoldInventory _customerGold;
    private MinerIdentity _customerIdentity;
    private FreeMinerCertificate _customerCertificate;
    private PlayerInteractionFocus _customerFocus;
    private string _customerName;
    private bool _panelOpen;

    public float CertificateFee => _certificateFee;
    public float ClaimRegistrationFee => _claimRegistrationFee;

    private void Awake()
    {
        if (_ui == null)
        {
            _ui = FindObjectOfType<GoldCommissionerOfficeUI>();
        }

        _claimManager = FindObjectOfType<ClaimManager>();
    }

    private void OnTriggerEnter(
        Collider other)
    {
        GoldInventory gold = other.GetComponentInParent<GoldInventory>();
        MinerIdentity identity = other.GetComponentInParent<MinerIdentity>();
        FreeMinerCertificate certificate =
            other.GetComponentInParent<FreeMinerCertificate>();

        if (gold == null || identity == null || certificate == null)
        {
            return;
        }

        _customerGold = gold;
        _customerIdentity = identity;
        _customerCertificate = certificate;
        _customerFocus = other.GetComponentInParent<PlayerInteractionFocus>();
        _customerFocus?.BeginEngagement();
        _customerName = other.transform.root.name;
        _panelOpen = false;
        RefreshUi();
    }

    private void OnTriggerExit(
        Collider other)
    {
        MinerIdentity identity = other.GetComponentInParent<MinerIdentity>();

        if (identity == null || identity != _customerIdentity)
        {
            return;
        }

        _customerGold = null;
        _customerIdentity = null;
        _customerCertificate = null;

        _customerFocus?.EndEngagement();
        _customerFocus = null;

        _panelOpen = false;

        if (_ui != null)
        {
            _ui.HideAll();
        }
    }

    private void Update()
    {
        if (_customerIdentity == null)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return;
        }

        if (keyboard[_tradeKey].wasPressedThisFrame)
        {
            _panelOpen = !_panelOpen;
        }

        if (_panelOpen)
        {
            if (keyboard[_certificateKey].wasPressedThisFrame)
            {
                TryIssueCertificate();
            }

            if (keyboard[_registerClaimKey].wasPressedThisFrame)
            {
                TryRegisterClaim();
            }
        }

        RefreshUi();
    }

    public bool TryIssueCertificate()
    {
        if (_customerGold == null || _customerCertificate == null)
        {
            return false;
        }

        if (!_customerGold.TrySpendGold(_certificateFee))
        {
            ShowMessage("Not enough gold for a Free Miner's Certificate.");
            return false;
        }

        _customerCertificate.Issue();
        ShowMessage($"{_customerName}'s Free Miner's Certificate is up to date.");
        return true;
    }

    public bool TryRegisterClaim()
    {
        if (_customerGold == null || _customerIdentity == null || _claimManager == null)
        {
            return false;
        }

        ClaimData pending =
            _claimManager.FindUnregisteredClaimOwnedBy(_customerIdentity);

        if (pending == null)
        {
            ShowMessage($"{_customerName} has no staked claim waiting to register.");
            return false;
        }

        if (!_customerCertificate.IsValid)
        {
            ShowMessage("You need a valid Free Miner's Certificate before registering a claim.");
            return false;
        }

        if (!_customerGold.TrySpendGold(_claimRegistrationFee))
        {
            ShowMessage("Not enough gold to register the claim.");
            return false;
        }

        _claimManager.TryRegisterClaim(pending, _claimRegistrationFee);
        ShowMessage($"\"{pending.ClaimName}\" is registered and active.");
        return true;
    }

    private void ShowMessage(
        string message)
    {
        Debug.Log($"[Gold Commissioner's Office] {message}");

        if (_ui != null)
        {
            _ui.ShowMessage(message);
        }
    }

    private void RefreshUi()
    {
        if (_ui == null)
        {
            return;
        }

        if (!_panelOpen)
        {
            _ui.ShowPrompt($"{_customerName}: Press {_tradeKey} to Trade");
            return;
        }

        bool hasCertificate =
            _customerCertificate != null && _customerCertificate.IsValid;

        ClaimData pending =
            _claimManager != null
                ? _claimManager.FindUnregisteredClaimOwnedBy(_customerIdentity)
                : null;

        _ui.ShowPanel(
            "Gold Commissioner's Office\n" +
            $"[{_certificateKey}] Free Miner's Certificate -- {_certificateFee:0.####} oz/yr " +
            (hasCertificate ? "(current)" : "(REQUIRED)") + "\n" +
            $"[{_registerClaimKey}] Register Claim -- {_claimRegistrationFee:0.####} oz " +
            (pending != null ? $"(\"{pending.ClaimName}\" waiting)" : "(nothing staked)") + "\n" +
            $"{_customerName}: {_customerGold.TotalGoldOunces:0.####} oz gold");
    }
}
