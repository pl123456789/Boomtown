/// <summary>
/// What ClaimManager tells a caller (GoldPanningController, UI, eventually
/// an AI claim jumper) about a specific world position. Deliberately an
/// enum rather than a bool so every caller gets the same vocabulary for
/// both the permission check and the message shown to the player.
/// </summary>
public enum AccessResult
{
    /// <summary>No claim covers this position -- free prospecting.</summary>
    Unclaimed,

    /// <summary>The requester owns the claim covering this position.</summary>
    Owner,

    /// <summary>The requester is an authorized worker on this claim.</summary>
    AuthorizedWorker,

    /// <summary>Someone else's active claim. Panning is blocked.</summary>
    OthersClaim,

    /// <summary>
    /// The requester owns or works this claim, but their Free Miner's
    /// Certificate has lapsed. Blocked until renewed at the Gold
    /// Commissioner's Office, even on your own ground.
    /// </summary>
    CertificateExpired,

    /// <summary>
    /// This claim was abandoned (unworked past the forfeiture window) and
    /// is open again -- treated the same as Unclaimed by panning, but kept
    /// as its own value so UI/AI can still say "this used to be someone's
    /// claim" if useful later.
    /// </summary>
    Forfeited
}
