using UnityEngine;

/// <summary>
/// Tracks an employee's personal needs — hunger and rest — decaying over time and
/// flagging when a need has gone unmet long enough to require attention.
/// Hunger is restored by eating grub; rest is restored by sleeping.
/// </summary>
[DisallowMultipleComponent]
public sealed class EmployeeNeeds : MonoBehaviour
{
    [Header("Hunger")]

    [SerializeField, Range(0f, 100f)]
    private float _hunger = 100f;

    [SerializeField, Min(0f)]
    private float _hungerDecayPerMinute = 2f;

    [SerializeField, Range(0f, 100f)]
    private float _hungerUnmetThreshold = 30f;

    [Header("Rest")]

    [SerializeField, Range(0f, 100f)]
    private float _rest = 100f;

    [SerializeField, Min(0f)]
    private float _restDecayPerMinute = 1.5f;

    [SerializeField, Range(0f, 100f)]
    private float _restUnmetThreshold = 30f;

    public float Hunger => _hunger;
    public float Rest => _rest;

    public bool IsHungerUnmet => _hunger <= _hungerUnmetThreshold;
    public bool IsRestUnmet => _rest <= _restUnmetThreshold;
    public bool HasUnmetNeed => IsHungerUnmet || IsRestUnmet;

    private void Update()
    {
        Decay(
            ref _hunger,
            _hungerDecayPerMinute);

        Decay(
            ref _rest,
            _restDecayPerMinute);
    }

    /// <summary>
    /// Restores hunger by eating grub. Amount is on the same 0-100 scale as Hunger.
    /// </summary>
    public void EatGrub(
        float amount)
    {
        _hunger =
            Mathf.Clamp(
                _hunger + amount,
                0f,
                100f);
    }

    /// <summary>
    /// Restores rest, e.g. after sleeping at a camp or home.
    /// </summary>
    public void Sleep(
        float amount)
    {
        _rest =
            Mathf.Clamp(
                _rest + amount,
                0f,
                100f);
    }

    private static void Decay(
        ref float value,
        float perMinute)
    {
        value =
            Mathf.Max(
                0f,
                value - perMinute * Time.deltaTime / 60f);
    }
}
