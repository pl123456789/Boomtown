using System;
using UnityEngine;

/// <summary>
/// A stable identity for anyone who can own or work a claim (the player,
/// an employee). Claims and certificates reference this component rather
/// than a name string, so renaming a character doesn't break ownership,
/// and there's a real ID ready for save/load or multiplayer later without
/// having to retrofit one in.
/// </summary>
[DisallowMultipleComponent]
public sealed class MinerIdentity : MonoBehaviour
{
    [SerializeField]
    private string _displayName;

    [SerializeField]
    private string _serializedId;

    private Guid _id;

    public Guid Id => _id;

    public string DisplayName =>
        string.IsNullOrEmpty(_displayName)
            ? gameObject.name
            : _displayName;

    private void Awake()
    {
        if (string.IsNullOrEmpty(_serializedId))
        {
            _serializedId = Guid.NewGuid().ToString();
        }

        _id = Guid.Parse(_serializedId);

        if (string.IsNullOrEmpty(_displayName))
        {
            _displayName = gameObject.name;
        }
    }

    /// <summary>
    /// Lets Employee/QuickPlayerController hand over the name they already
    /// track (e.g. "Theodore Parsons") instead of duplicating it.
    /// </summary>
    public void SetDisplayName(
        string displayName)
    {
        if (!string.IsNullOrEmpty(displayName))
        {
            _displayName = displayName;
        }
    }
}
