using Boomtown.Gameplay.Prospecting;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// A simple general store: walk into its trigger volume, press the trade
/// key to open a buy/sell panel for grub. Works for any character carrying
/// both a GoldInventory and a GrubInventory (Bill or an employee like Ted),
/// so the player can also walk an employee over and manage their supplies.
/// </summary>
[DisallowMultipleComponent]
public sealed class GeneralStoreController : MonoBehaviour
{
    [Header("Prices (gold ounces per unit)")]

    [SerializeField, Min(0f)]
    private float _grubBuyPrice = 0.01f;

    [SerializeField, Min(0f)]
    private float _grubSellPrice = 0.005f;

    [Header("Interaction")]

    [SerializeField]
    private Key _tradeKey = Key.T;

    [SerializeField]
    private Key _buyKey = Key.B;

    [SerializeField]
    private Key _sellKey = Key.G;

    [Header("UI")]

    [SerializeField]
    private GeneralStoreUI _ui;

    private GoldInventory _customerGold;
    private GrubInventory _customerGrub;
    private PlayerInteractionFocus _customerFocus;
    private string _customerName;
    private bool _panelOpen;

    public float GrubBuyPrice => _grubBuyPrice;
    public float GrubSellPrice => _grubSellPrice;

    private void Awake()
    {
        if (_ui == null)
        {
            _ui = FindObjectOfType<GeneralStoreUI>();
        }
    }

    private void OnTriggerEnter(
        Collider other)
    {
        GoldInventory gold = other.GetComponentInParent<GoldInventory>();
        GrubInventory grub = other.GetComponentInParent<GrubInventory>();

        if (gold == null || grub == null)
        {
            return;
        }

        _customerGold = gold;
        _customerGrub = grub;
        _customerFocus = other.GetComponentInParent<PlayerInteractionFocus>();
        _customerFocus?.BeginEngagement();
        _customerName = other.transform.root.name;
        _panelOpen = false;
        RefreshUi();
    }

    private void OnTriggerExit(
        Collider other)
    {
        GoldInventory gold = other.GetComponentInParent<GoldInventory>();

        if (gold == null || gold != _customerGold)
        {
            return;
        }

        _customerGold = null;
        _customerGrub = null;
        _panelOpen = false;

        _customerFocus?.EndEngagement();
        _customerFocus = null;

        if (_ui != null)
        {
            _ui.HideAll();
        }
    }

    private void Update()
    {
        if (_customerGold == null || _customerGrub == null)
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
            if (keyboard[_buyKey].wasPressedThisFrame)
            {
                TryBuyGrub(1);
            }

            if (keyboard[_sellKey].wasPressedThisFrame)
            {
                TrySellGrub(1);
            }
        }

        RefreshUi();
    }

    public bool TryBuyGrub(
        int quantity)
    {
        if (_customerGold == null || _customerGrub == null || quantity <= 0)
        {
            return false;
        }

        float cost = quantity * _grubBuyPrice;

        if (!_customerGold.TrySpendGold(cost))
        {
            return false;
        }

        _customerGrub.AddGrub(quantity);
        return true;
    }

    public bool TrySellGrub(
        int quantity)
    {
        if (_customerGold == null || _customerGrub == null || quantity <= 0)
        {
            return false;
        }

        if (!_customerGrub.TryRemoveGrub(quantity))
        {
            return false;
        }

        _customerGold.AddGold(quantity * _grubSellPrice);
        return true;
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

        _ui.ShowPanel(
            "General Store\n" +
            $"[{_buyKey}] Buy grub -- {_grubBuyPrice:0.####} oz each\n" +
            $"[{_sellKey}] Sell grub -- {_grubSellPrice:0.####} oz each\n" +
            $"{_customerName}: {_customerGrub.GrubCount} grub, " +
            $"{_customerGold.TotalGoldOunces:0.####} oz gold");
    }
}
