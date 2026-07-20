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

        [ContextMenu("Clear Gold")]
        public void ClearGold()
        {
            totalGoldOunces = 0f;
            GoldChanged?.Invoke(totalGoldOunces);
        }
    }
}
