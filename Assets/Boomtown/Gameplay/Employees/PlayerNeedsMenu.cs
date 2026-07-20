using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Lets the player manage Bill's own hunger and rest on demand. Press the
/// menu key any time for two options: "Eat Some Grub" (instant, costs 1
/// grub from inventory -- eating already has a cost, buying the grub) and
/// "Get Some Shuteye" (free, but takes a few seconds so it can't be spammed
/// for an instant full heal the way eating already can't be, since it's
/// capped by how much grub you're carrying).
/// </summary>
public sealed class PlayerNeedsMenu : MonoBehaviour
{
    [Header("Interaction")]

    [SerializeField]
    private Key _menuKey = Key.N;

    [SerializeField]
    private Key _eatKey = Key.E;

    [SerializeField]
    private Key _restKey = Key.R;

    [Header("Shuteye")]

    [SerializeField, Min(0.1f)]
    private float _restDuration = 3f;

    [SerializeField, Min(0f)]
    private float _restRestored = 45f;

    [Header("UI")]

    [SerializeField]
    private PlayerNeedsUI _ui;

    private QuickPlayerController _player;
    private bool _menuOpen;
    private bool _isResting;

    private void Awake()
    {
        _player = GetComponent<QuickPlayerController>();

        if (_ui == null)
        {
            _ui = FindObjectOfType<PlayerNeedsUI>();
        }
    }

    private void Update()
    {
        if (_isResting)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return;
        }

        if (keyboard[_menuKey].wasPressedThisFrame)
        {
            _menuOpen = !_menuOpen;
        }

        if (_menuOpen)
        {
            if (keyboard[_eatKey].wasPressedThisFrame)
            {
                _player.TryEatGrub();
            }

            if (keyboard[_restKey].wasPressedThisFrame)
            {
                StartCoroutine(RestRoutine());
                return;
            }
        }

        RefreshUi();
    }

    private IEnumerator RestRoutine()
    {
        _isResting = true;
        _menuOpen = false;

        if (_ui != null)
        {
            _ui.ShowResting();
        }

        yield return new WaitForSeconds(_restDuration);

        _player.Needs.Sleep(_restRestored);
        _isResting = false;

        if (_ui != null)
        {
            _ui.HideAll();
        }
    }

    private void RefreshUi()
    {
        if (_ui == null)
        {
            return;
        }

        if (!_menuOpen)
        {
            _ui.HideAll();
            return;
        }

        _ui.ShowMenu(
            $"[{_eatKey}] Eat Some Grub  (have {_player.Grub.GrubCount})\n" +
            $"[{_restKey}] Get Some Shuteye\n" +
            $"Hunger {_player.Needs.Hunger:0}   Rest {_player.Needs.Rest:0}");
    }
}
