/// <summary>
/// A claim's lifecycle. Forfeiture is a progression, not a single instant
/// deletion, so the player gets warning states before losing a claim.
/// </summary>
public enum ClaimState
{
    /// <summary>
    /// Staked (corner posts placed) but not yet registered and paid for at
    /// the Gold Commissioner's Office. Not exclusive yet -- anyone can
    /// still pan here, and another player could stake over it. Kept out of
    /// the permanent claim widget; only shown while actively staking.
    /// </summary>
    Unregistered,

    /// <summary>Registered, fee paid, worked within the last 72 hours.</summary>
    Active,

    /// <summary>Past the 72-hour representation window. First warning.</summary>
    RepresentationOverdue,

    /// <summary>Further overdue. Strong warning -- forfeiture is close.</summary>
    AtRisk,

    /// <summary>
    /// Unworked long enough to be forfeited. Ownership no longer applies;
    /// CheckAccess treats this the same as Unclaimed.
    /// </summary>
    Forfeited
}
