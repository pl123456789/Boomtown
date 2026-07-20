using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Lets a prospector stake a claim by walking to the bank and planting two
/// posts. The first post plus the second set the frontage (width and
/// orientation, matching the historical "so many feet of bank"); the far
/// two corners are computed automatically at a fixed depth into the bank,
/// so the player never has to place a self-intersecting or degenerate
/// shape. A rope line previews the frontage while placing, then the
/// finished quad is drawn (see ClaimData) once staked.
///
/// Staking alone does not grant exclusivity -- the resulting ClaimData
/// starts Unregistered. The player still has to visit the Gold
/// Commissioner's Office to pay the fee and make it Active.
/// </summary>
[DisallowMultipleComponent]
public sealed class ClaimStakingController : MonoBehaviour
{
    [Header("Interaction")]

    [SerializeField]
    private Key _stakeKey = Key.K;

    [Header("Claim Shape")]

    [SerializeField, Min(1f)]
    private float _claimDepth = 25f;

    [SerializeField, Min(1f)]
    private float _minimumFrontage = 15f;

    [SerializeField, Min(1f)]
    private float _maximumFrontage = 40f;

    [Header("UI")]

    [SerializeField]
    private ClaimStakingUI _ui;

    private MinerIdentity _identity;
    private ClaimManager _claimManager;
    private PlayerInteractionFocus _focus;
    private Boomtown.Gameplay.Prospecting.GoldPanningController _panningController;

    private bool _isStaking;
    private Vector3? _firstPost;
    private LineRenderer _previewLine;

    private void Awake()
    {
        _identity = GetComponent<MinerIdentity>();
        _focus = GetComponent<PlayerInteractionFocus>();
        _panningController = GetComponent<Boomtown.Gameplay.Prospecting.GoldPanningController>();

        if (_ui == null)
        {
            _ui = FindObjectOfType<ClaimStakingUI>();
        }
    }

    /// <summary>
    /// True while the player is engaged with something else that already
    /// owns the ambient world-prompt slot -- a trade panel, or actively
    /// panning (including the result-display window right after). The
    /// staking prompt has no location gating of its own, so without this
    /// it renders on top of whatever the player's actually looking at.
    /// </summary>
    private bool IsSuppressedByOtherInteraction()
    {
        return (_focus != null && _focus.IsEngaged) ||
               (_panningController != null && _panningController.IsPanning);
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return;
        }

        if (keyboard[_stakeKey].wasPressedThisFrame)
        {
            HandleStakeKeyPress();
        }

        if (_isStaking && _firstPost.HasValue)
        {
            UpdatePreviewLine(_firstPost.Value, transform.position);
        }

        RefreshPrompt();
    }

    private void HandleStakeKeyPress()
    {
        if (!_isStaking)
        {
            if (IsSuppressedByOtherInteraction())
            {
                return;
            }

            BeginStaking();
            return;
        }

        if (!_firstPost.HasValue)
        {
            _firstPost = transform.position;
            return;
        }

        TryCompleteStaking(_firstPost.Value, transform.position);
    }

    private void BeginStaking()
    {
        FindManager();

        _isStaking = true;
        _firstPost = null;
        EnsurePreviewLine();
    }

    private void TryCompleteStaking(
        Vector3 firstPost,
        Vector3 secondPost)
    {
        _isStaking = false;
        _firstPost = null;
        HidePreviewLine();

        float frontage = Vector3.Distance(
            new Vector3(firstPost.x, 0f, firstPost.z),
            new Vector3(secondPost.x, 0f, secondPost.z));

        if (frontage < _minimumFrontage)
        {
            ShowMessage(
                $"That's too narrow for a claim -- walk at least " +
                $"{_minimumFrontage:0} feet before planting the second post.");

            return;
        }

        if (frontage > _maximumFrontage)
        {
            ShowMessage(
                $"That's more bank than one claim is allowed -- keep it " +
                $"under {_maximumFrontage:0} feet.");

            return;
        }

        Vector3[] corners = BuildCorners(firstPost, secondPost);
        Vector3 midpoint = (corners[0] + corners[1] + corners[2] + corners[3]) / 4f;

        if (_claimManager == null)
        {
            ShowMessage("No claim office is set up yet -- can't stake a claim.");
            return;
        }

        ClaimData existing = _claimManager.FindClaimContaining(midpoint);

        if (existing != null &&
            existing.State != ClaimState.Forfeited &&
            existing.State != ClaimState.Unregistered)
        {
            ShowMessage($"That ground is already claimed: {existing.ClaimName}.");
            return;
        }

        ClaimData claim = _claimManager.CreateUnregisteredClaim(
            corners,
            $"{_identity.DisplayName}'s Claim",
            _identity);

        ShowMessage(
            claim != null
                ? $"Staked \"{claim.ClaimName}.\" Visit the Gold " +
                  "Commissioner's Office to register it."
                : "Couldn't stake a claim there.");
    }

    /// <summary>
    /// Frontage posts A and B, then C and D computed at _claimDepth into
    /// whichever perpendicular direction is the higher (inland) side of
    /// the bank -- avoids depending on any editor-only world-generation
    /// code, which isn't available at runtime.
    /// </summary>
    private Vector3[] BuildCorners(
        Vector3 postA,
        Vector3 postB)
    {
        Vector3 frontage = postB - postA;
        frontage.y = 0f;

        Vector3 perpendicular =
            new Vector3(-frontage.z, 0f, frontage.x).normalized;

        Vector3 midpoint = (postA + postB) * 0.5f;
        Vector3 inlandDirection = perpendicular;

        Terrain terrain = Terrain.activeTerrain;

        if (terrain != null)
        {
            float heightPositive = terrain.SampleHeight(
                midpoint + perpendicular * _claimDepth);

            float heightNegative = terrain.SampleHeight(
                midpoint - perpendicular * _claimDepth);

            inlandDirection =
                heightPositive >= heightNegative
                    ? perpendicular
                    : -perpendicular;
        }

        Vector3 postC = postB + inlandDirection * _claimDepth;
        Vector3 postD = postA + inlandDirection * _claimDepth;

        if (terrain != null)
        {
            postA.y = terrain.SampleHeight(postA) + terrain.transform.position.y;
            postB.y = terrain.SampleHeight(postB) + terrain.transform.position.y;
            postC.y = terrain.SampleHeight(postC) + terrain.transform.position.y;
            postD.y = terrain.SampleHeight(postD) + terrain.transform.position.y;
        }

        return new[] { postA, postB, postC, postD };
    }

    private void FindManager()
    {
        if (_claimManager == null)
        {
            _claimManager = FindObjectOfType<ClaimManager>();
        }
    }

    private void EnsurePreviewLine()
    {
        if (_previewLine != null)
        {
            return;
        }

        GameObject lineObject = new("ClaimStakingPreview");
        lineObject.transform.SetParent(transform, false);

        _previewLine = lineObject.AddComponent<LineRenderer>();
        _previewLine.useWorldSpace = true;
        _previewLine.positionCount = 2;
        _previewLine.widthMultiplier = 0.12f;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        _previewLine.material = new Material(shader);
        _previewLine.startColor = Color.white;
        _previewLine.endColor = Color.white;
        _previewLine.enabled = false;
    }

    private void UpdatePreviewLine(
        Vector3 from,
        Vector3 to)
    {
        if (_previewLine == null)
        {
            return;
        }

        from.y += 0.15f;
        to.y += 0.15f;

        _previewLine.enabled = true;
        _previewLine.SetPosition(0, from);
        _previewLine.SetPosition(1, to);
    }

    private void HidePreviewLine()
    {
        if (_previewLine != null)
        {
            _previewLine.enabled = false;
        }
    }

    private void RefreshPrompt()
    {
        if (_ui == null)
        {
            return;
        }

        if (!_isStaking)
        {
            if (IsSuppressedByOtherInteraction())
            {
                _ui.HidePrompt();
                return;
            }

            _ui.ShowPrompt($"[{_stakeKey}] Stake a Claim");
            return;
        }

        _ui.ShowPrompt(
            !_firstPost.HasValue
                ? $"[{_stakeKey}] Plant the first post"
                : $"[{_stakeKey}] Plant the second post");
    }

    private void ShowMessage(
        string message)
    {
        Debug.Log($"[Claim Staking] {message}");

        if (_ui != null)
        {
            _ui.ShowMessage(message);
        }
    }
}
