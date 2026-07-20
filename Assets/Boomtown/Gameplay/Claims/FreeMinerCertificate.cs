using UnityEngine;

/// <summary>
/// The historical Free Miner's Certificate -- belongs to the miner, not to
/// any particular claim. Issued (and renewed) at the Gold Commissioner's
/// Office for a fee. A claim owner or worker without a valid certificate
/// is blocked from panning even on their own ground (see
/// AccessResult.CertificateExpired).
///
/// Expiry is tracked in real seconds of play time rather than an in-game
/// calendar date, since the project doesn't have a day/night or calendar
/// system yet. The default duration is intentionally generous (won't lapse
/// mid-session) so certificate expiry doesn't fight the more interesting
/// 72-hour claim representation timer for the player's attention. Swap
/// this for real calendar dates once a calendar system exists.
/// </summary>
[DisallowMultipleComponent]
public sealed class FreeMinerCertificate : MonoBehaviour
{
    [SerializeField, Min(0f)]
    private float _validDurationSeconds = 3600f;

    private bool _hasBeenIssued;
    private float _issuedAtTime;

    public bool HasBeenIssued => _hasBeenIssued;

    public bool IsValid =>
        _hasBeenIssued &&
        Time.time - _issuedAtTime < _validDurationSeconds;

    public float ExpiresInSeconds =>
        _hasBeenIssued
            ? Mathf.Max(
                0f,
                _validDurationSeconds -
                (Time.time - _issuedAtTime))
            : 0f;

    /// <summary>Issues or renews the certificate as of right now.</summary>
    public void Issue()
    {
        _hasBeenIssued = true;
        _issuedAtTime = Time.time;
    }
}
