using System;
using UnityEngine;

namespace Boomtown.Gameplay.Prospecting
{
    public sealed class GoldInventory : MonoBehaviour
    {
        [SerializeField, Min(0f)]
        private float totalGoldOunces;

        public float TotalGoldOunces => totalGoldOunces;

        public event Action<float> GoldChanged;

        public void AddGold(float ounces)
        {
            if (ounces <= 0f)
            {
                return;
            }

            totalGoldOunces += ounces;
            GoldChanged?.Invoke(totalGoldOunces);
        }

        /// <summary>
        /// Spends gold, e.g. buying grub at the General Store. Fails and
        /// leaves the balance untouched if there isn't enough on hand.
        /// </summary>
        public bool TrySpendGold(float ounces)
        {
            if (ounces <= 0f || totalGoldOunces < ounces)
            {
                return false;
            }

            totalGoldOunces -= ounces;
            GoldChanged?.Invoke(totalGoldOunces);
            return true;
        }

        [ContextMenu("Clear Gold")]
        public void ClearGold()
        {
            totalGoldOunces = 0f;
            GoldChanged?.Invoke(totalGoldOunces);
        }
    }
}
