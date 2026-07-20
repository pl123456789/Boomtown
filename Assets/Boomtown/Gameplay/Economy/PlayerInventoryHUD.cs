using Boomtown.Gameplay.Prospecting;
using TMPro;
using UnityEngine;

/// <summary>
/// Persistent HUD text showing Bill's gold and grub totals. Finds him
/// automatically, so it can be dropped anywhere under the gameplay canvas
/// with no manual wiring required.
/// </summary>
public sealed class PlayerInventoryHUD : MonoBehaviour
{
    [SerializeField]
    private TMP_Text _text;

    private GoldInventory _gold;
    private GrubInventory _grub;

    private void Start()
    {
        QuickPlayerController player =
            FindObjectOfType<QuickPlayerController>();

        if (player == null)
        {
            return;
        }

        _gold = player.GetComponent<GoldInventory>();
        _grub = player.Grub;

        if (_gold != null)
        {
            _gold.GoldChanged += OnGoldChanged;
        }

        if (_grub != null)
        {
            _grub.GrubChanged += OnGrubChanged;
        }

        Refresh();
    }

    private void OnDestroy()
    {
        if (_gold != null)
        {
            _gold.GoldChanged -= OnGoldChanged;
        }

        if (_grub != null)
        {
            _grub.GrubChanged -= OnGrubChanged;
        }
    }

    private void OnGoldChanged(
        float _)
    {
        Refresh();
    }

    private void OnGrubChanged(
        int _)
    {
        Refresh();
    }

    private void Refresh()
    {
        if (_text == null)
        {
            return;
        }

        float gold = _gold != null ? _gold.TotalGoldOunces : 0f;
        int grub = _grub != null ? _grub.GrubCount : 0;

        _text.text = $"Gold: {gold:0.####} oz\nGrub: {grub}";
    }
}
