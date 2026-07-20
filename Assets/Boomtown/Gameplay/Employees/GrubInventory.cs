using System;
using UnityEngine;

/// <summary>
/// Tracks how many units of grub a character is personally carrying.
/// Grub is bought and sold at the General Store and consumed to restore
/// hunger (see EmployeeNeeds.EatGrub). Per-character, same as GoldInventory --
/// Bill and every employee keep their own separate stock.
/// </summary>
[DisallowMultipleComponent]
public sealed class GrubInventory : MonoBehaviour
{
    [SerializeField, Min(0)]
    private int _grubCount;

    public int GrubCount => _grubCount;

    public event Action<int> GrubChanged;

    public void AddGrub(
        int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        _grubCount += amount;
        GrubChanged?.Invoke(_grubCount);
    }

    public bool TryRemoveGrub(
        int amount)
    {
        if (amount <= 0 || _grubCount < amount)
        {
            return false;
        }

        _grubCount -= amount;
        GrubChanged?.Invoke(_grubCount);
        return true;
    }

    [ContextMenu("Clear Grub")]
    public void ClearGrub()
    {
        _grubCount = 0;
        GrubChanged?.Invoke(_grubCount);
    }
}
