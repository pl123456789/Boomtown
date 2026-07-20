using UnityEngine;

/// <summary>
/// Tracks whether this character is currently engaged with a walk-up
/// trade panel (General Store, Gold Commissioner's Office, and any future
/// building like it). Other ambient world prompts -- like the claim
/// staking prompt, which has no location gating of its own -- check this
/// before showing themselves, so they don't render on top of whatever
/// the player is actually looking at.
///
/// A count instead of a bool so two overlapping trigger volumes (e.g. a
/// player standing where two buildings' interaction ranges happen to
/// overlap) can't leave this stuck "off" when only one of them exits.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerInteractionFocus : MonoBehaviour
{
    private int _engagementCount;

    public bool IsEngaged => _engagementCount > 0;

    public void BeginEngagement()
    {
        _engagementCount++;
    }

    public void EndEngagement()
    {
        _engagementCount = Mathf.Max(0, _engagementCount - 1);
    }
}
