using UnityEngine;

/// <summary>
/// Swings a stickman's shoulder/hip pivots in a walk-cycle sine pattern, and
/// folds the elbow/knee pivots during each limb's swing phase, based on how
/// fast its root transform is actually moving on the ground plane.
/// No skeleton, no Animator — works directly against the LeftArm/RightArm/
/// LeftLeg/RightLeg pivot groups (and their nested LeftElbow/RightElbow/
/// LeftKnee/RightKnee pivots) the Character Randomizer builds under
/// Visual/Body. Self-contained: attach to Bill or Ted and it finds its own
/// limbs, so it doesn't care whether a player or an AI mover is driving them.
/// </summary>
[DisallowMultipleComponent]
public sealed class StickFigureWalkAnimator : MonoBehaviour
{
    [Header("Limb Search")]

    [SerializeField]
    private string _visualRootName = "Visual";

    [SerializeField]
    private string _bodyGroupName = "Body";

    [Header("Gait")]

    [SerializeField, Min(0f)]
    private float _swingAngleDegrees = 35f;

    [SerializeField, Min(0.05f)]
    private float _strideLengthMeters = 1.1f;

    [SerializeField, Min(0f)]
    private float _poseLerpSpeed = 10f;

    [SerializeField, Min(0f)]
    private float _minSpeedToWalk = 0.1f;

    [Header("Joint Bend")]

    [SerializeField, Min(0f)]
    private float _kneeBendDegrees = 50f;

    [SerializeField, Min(0f)]
    private float _elbowBendDegrees = 20f;

    private Transform _leftArm;
    private Transform _rightArm;
    private Transform _leftLeg;
    private Transform _rightLeg;
    private Transform _leftElbow;
    private Transform _rightElbow;
    private Transform _leftKnee;
    private Transform _rightKnee;

    private Quaternion _leftArmRest;
    private Quaternion _rightArmRest;
    private Quaternion _leftLegRest;
    private Quaternion _rightLegRest;
    private Quaternion _leftElbowRest;
    private Quaternion _rightElbowRest;
    private Quaternion _leftKneeRest;
    private Quaternion _rightKneeRest;

    private Vector3 _previousPosition;
    private float _phaseDegrees;
    private float _currentSwing;

    private void Start()
    {
        FindLimbs();
        _previousPosition = transform.position;
    }

    private void Update()
    {
        if (_leftArm == null)
        {
            return;
        }

        float speed = MeasureHorizontalSpeed();
        bool isWalking = speed > _minSpeedToWalk;

        float targetSwing = isWalking ? _swingAngleDegrees : 0f;
        _currentSwing = Mathf.Lerp(
            _currentSwing,
            targetSwing,
            _poseLerpSpeed * Time.deltaTime);

        if (isWalking)
        {
            float strideRate = (speed / _strideLengthMeters) * 360f;
            _phaseDegrees += strideRate * Time.deltaTime;
        }

        // 0 when idle, ramps to 1 while walking -- fades joint bend in/out
        // together with the shoulder/hip swing instead of snapping.
        float walkBlend = _swingAngleDegrees > 0f
            ? _currentSwing / _swingAngleDegrees
            : 0f;

        float leftLegPhase = _phaseDegrees;
        float rightLegPhase = _phaseDegrees + 180f;

        float leftSwing = Mathf.Sin(leftLegPhase * Mathf.Deg2Rad) * _currentSwing;
        float rightSwing = Mathf.Sin(rightLegPhase * Mathf.Deg2Rad) * _currentSwing;

        // Contralateral gait: left leg forward pairs with right arm forward.
        ApplySwing(_leftLeg, _leftLegRest, leftSwing);
        ApplySwing(_rightLeg, _rightLegRest, rightSwing);
        ApplySwing(_leftArm, _leftArmRest, rightSwing);
        ApplySwing(_rightArm, _rightArmRest, leftSwing);

        // Knees/elbows fold (one direction only) during each limb's own
        // swing phase, then straighten out again as it plants.
        float leftKneeBend = _kneeBendDegrees *
            Mathf.Max(0f, Mathf.Cos(leftLegPhase * Mathf.Deg2Rad)) * walkBlend;
        float rightKneeBend = _kneeBendDegrees *
            Mathf.Max(0f, Mathf.Cos(rightLegPhase * Mathf.Deg2Rad)) * walkBlend;
        float leftElbowBend = _elbowBendDegrees *
            Mathf.Max(0f, Mathf.Cos(rightLegPhase * Mathf.Deg2Rad)) * walkBlend;
        float rightElbowBend = _elbowBendDegrees *
            Mathf.Max(0f, Mathf.Cos(leftLegPhase * Mathf.Deg2Rad)) * walkBlend;

        ApplySwing(_leftKnee, _leftKneeRest, leftKneeBend);
        ApplySwing(_rightKnee, _rightKneeRest, rightKneeBend);
        ApplySwing(_leftElbow, _leftElbowRest, -leftElbowBend);
        ApplySwing(_rightElbow, _rightElbowRest, -rightElbowBend);
    }

    private float MeasureHorizontalSpeed()
    {
        Vector3 position = transform.position;
        Vector3 delta = position - _previousPosition;
        delta.y = 0f;
        _previousPosition = position;

        return Time.deltaTime > 0f ? delta.magnitude / Time.deltaTime : 0f;
    }

    private static void ApplySwing(
        Transform limb,
        Quaternion restRotation,
        float angleDegrees)
    {
        if (limb == null)
        {
            return;
        }

        limb.localRotation = restRotation * Quaternion.Euler(angleDegrees, 0f, 0f);
    }

    private void FindLimbs()
    {
        Transform visual = transform.Find(_visualRootName);
        Transform body = visual != null ? visual.Find(_bodyGroupName) : null;

        if (body == null)
        {
            return;
        }

        _leftArm = body.Find("LeftArm");
        _rightArm = body.Find("RightArm");
        _leftLeg = body.Find("LeftLeg");
        _rightLeg = body.Find("RightLeg");

        _leftElbow = _leftArm != null ? _leftArm.Find("LeftElbow") : null;
        _rightElbow = _rightArm != null ? _rightArm.Find("RightElbow") : null;
        _leftKnee = _leftLeg != null ? _leftLeg.Find("LeftKnee") : null;
        _rightKnee = _rightLeg != null ? _rightLeg.Find("RightKnee") : null;

        if (_leftArm != null) _leftArmRest = _leftArm.localRotation;
        if (_rightArm != null) _rightArmRest = _rightArm.localRotation;
        if (_leftLeg != null) _leftLegRest = _leftLeg.localRotation;
        if (_rightLeg != null) _rightLegRest = _rightLeg.localRotation;
        if (_leftElbow != null) _leftElbowRest = _leftElbow.localRotation;
        if (_rightElbow != null) _rightElbowRest = _rightElbow.localRotation;
        if (_leftKnee != null) _leftKneeRest = _leftKnee.localRotation;
        if (_rightKnee != null) _rightKneeRest = _rightKnee.localRotation;
    }
}
