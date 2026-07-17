using System;
using System.Collections;
using System.Collections.Generic;
using Boomtown.WorldGeneration;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace Boomtown.Gameplay.Prospecting
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(ProspectingLocationSensor))]
    [RequireComponent(typeof(GoldInventory))]
    public sealed class GoldPanningController : MonoBehaviour
    {
        private const float GrainsPerTroyOunce = 480f;

        [Header("Generated Geology")]

        [SerializeField]
        private BoomtownGeologyData geologyData;

        [Header("Panning")]

        [SerializeField, Min(0.1f)]
        private float panningDuration = 4f;

        [SerializeField, Min(0f)]
        private float resultDisplayDuration = 2.5f;

        [SerializeField, Min(0f)]
        private float completionHoldDuration = 0.12f;

        [SerializeField, Min(0f)]
        private float turnSmoothing = 9f;

        [SerializeField]
        private AnimationCurve progressCurve =
            AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [SerializeField]
        private Key panKey = Key.Space;

        [SerializeField]
        private PanResultEvaluator resultEvaluator = new();

        [Header("UI")]

        [SerializeField]
        private bool showPlayerUI = true;

        [SerializeField]
        private GoldPanningUI ui;

        private NavMeshAgent agent;
        private ProspectingLocationSensor sensor;
        private GoldInventory inventory;

        private readonly List<Behaviour> disabledMovementBehaviours = new();

        private bool isPanning;
        private Action queuedCompletion;

        public bool IsPanning => isPanning;

        public void AssignGeologyData(
            BoomtownGeologyData generatedGeology)
        {
            geologyData = generatedGeology;
        }

        public bool CanPanAt(Vector3 worldPosition)
        {
            return sensor != null &&
                   sensor.CanPanAt(worldPosition);
        }

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            sensor = GetComponent<ProspectingLocationSensor>();
            inventory = GetComponent<GoldInventory>();
        }

        private void Start()
        {
            if (geologyData == null)
            {
                Debug.LogError(
                    "[Gold Panning] BoomtownGeologyData is not assigned.",
                    this);
            }

            if (showPlayerUI &&
                ui == null)
            {
                Debug.LogError(
                    "[Gold Panning] GoldPanningUI is not assigned.",
                    this);
            }
        }

        private void Update()
        {
            if (isPanning)
            {
                return;
            }

            UpdatePrompt();

            Keyboard keyboard = Keyboard.current;

            bool shiftPressed =
                keyboard != null &&
                (keyboard.leftShiftKey.isPressed ||
                 keyboard.rightShiftKey.isPressed);

            if (sensor != null &&
                sensor.CanPanHere &&
                keyboard != null &&
                !shiftPressed &&
                keyboard[panKey].wasPressedThisFrame)
            {
                TryStartPanning();
            }
        }

        private void OnDisable()
        {
            if (!isPanning)
            {
                return;
            }

            RestoreMovement();
            isPanning = false;
            queuedCompletion = null;

            if (showPlayerUI &&
                ui != null)
            {
                ui.HideAll();
            }
        }

        public bool TryStartPanning()
        {
            return TryStartPanning(null);
        }

        public bool TryStartPanning(
            Action onCompleted)
        {
            if (sensor != null)
            {
                sensor.Refresh();
            }

            if (isPanning ||
                geologyData == null ||
                (showPlayerUI && ui == null) ||
                sensor == null ||
                !sensor.CanPanHere)
            {
                return false;
            }

            queuedCompletion = onCompleted;
            StartCoroutine(PanRoutine());
            return true;
        }

        private IEnumerator PanRoutine()
        {
            isPanning = true;
            StopAndLockMovement();

            if (showPlayerUI)
            {
                ui.BeginProgress();
            }

            yield return null;

            float elapsed = 0f;

            while (elapsed < panningDuration)
            {
                elapsed += Time.deltaTime;

                float normalized =
                    Mathf.Clamp01(
                        elapsed / panningDuration);

                float progress =
                    progressCurve != null
                        ? Mathf.Clamp01(
                            progressCurve.Evaluate(normalized))
                        : normalized;

                RotateTowardSamplePoint();

                if (showPlayerUI)
                {
                    ui.ShowProgress(progress);
                }

                yield return null;
            }

            if (showPlayerUI)
            {
                ui.ShowProgress(1f);
            }

            if (completionHoldDuration > 0f)
            {
                yield return new WaitForSeconds(
                    completionHoldDuration);
            }

            GoldExtractionResult extraction =
                BoomtownGoldExtractionService.Pan(
                    geologyData,
                    sensor.SamplePoint,
                    materialProcessed: 1f,
                    recoveryEfficiency: 0.65f);

            PanOutcome outcome =
                resultEvaluator.Evaluate(
                    extraction.grade);

            inventory.AddGold(
                extraction.extractedOunces);

            string resultText =
                BuildResultText(
                    outcome,
                    extraction.extractedOunces,
                    inventory.TotalGoldOunces);

            if (showPlayerUI)
            {
                ui.ShowResult(resultText);
            }

            Debug.Log(
                $"[Gold Panning] {resultText} | " +
                $"Geology grade: {extraction.grade:0.000} | " +
                $"Deposit remaining: {extraction.remainingOunces:0.####} oz",
                this);

            if (resultDisplayDuration > 0f)
            {
                yield return new WaitForSeconds(
                    resultDisplayDuration);
            }

            RestoreMovement();
            isPanning = false;

            Action completion = queuedCompletion;
            queuedCompletion = null;

            completion?.Invoke();
            UpdatePrompt();
        }

        private void RotateTowardSamplePoint()
        {
            if (sensor == null ||
                turnSmoothing <= 0f)
            {
                return;
            }

            Vector3 direction =
                sensor.SamplePoint -
                transform.position;

            direction.y = 0f;

            if (direction.sqrMagnitude < 0.001f)
            {
                return;
            }

            Quaternion targetRotation =
                Quaternion.LookRotation(
                    direction.normalized,
                    Vector3.up);

            float smoothing =
                1f - Mathf.Exp(
                    -turnSmoothing *
                    Time.deltaTime);

            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    smoothing);
        }

        private void UpdatePrompt()
        {
            if (!showPlayerUI ||
                ui == null)
            {
                return;
            }

            if (geologyData == null ||
                sensor == null ||
                !sensor.CanPanHere)
            {
                ui.HideAll();
                return;
            }

            ui.ShowPrompt(
                $"Press {panKey} to Pan for Gold");
        }

        private void StopAndLockMovement()
        {
            if (agent != null &&
                agent.enabled &&
                agent.isOnNavMesh)
            {
                agent.ResetPath();
                agent.velocity = Vector3.zero;
                agent.isStopped = true;
            }

            disabledMovementBehaviours.Clear();

            MonoBehaviour[] behaviours =
                GetComponents<MonoBehaviour>();

            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour == null ||
                    behaviour == this ||
                    behaviour == sensor ||
                    behaviour == inventory)
                {
                    continue;
                }

                string typeName =
                    behaviour.GetType().Name;

                bool isMovementBehaviour =
                    typeName == "QuickPlayerController" ||
                    typeName == "EmployeeMovement" ||
                    typeName == "WaypointPath";

                if (!isMovementBehaviour ||
                    !behaviour.enabled)
                {
                    continue;
                }

                behaviour.enabled = false;
                disabledMovementBehaviours.Add(behaviour);
            }
        }

        private void RestoreMovement()
        {
            foreach (Behaviour behaviour
                     in disabledMovementBehaviours)
            {
                if (behaviour != null)
                {
                    behaviour.enabled = true;
                }
            }

            disabledMovementBehaviours.Clear();

            if (agent != null &&
                agent.enabled &&
                agent.isOnNavMesh)
            {
                agent.velocity = Vector3.zero;
                agent.isStopped = false;
            }
        }

        private static string BuildResultText(
            PanOutcome outcome,
            float extractedOunces,
            float totalGoldOunces)
        {
            string reward =
                extractedOunces > 0f
                    ? $"+{FormatGold(extractedOunces)}"
                    : "No gold";

            return
                $"{outcome.Description}  {reward}\n" +
                $"Total Gold: {FormatGold(totalGoldOunces)}";
        }

        private static string FormatGold(float ounces)
        {
            float grains =
                ounces * GrainsPerTroyOunce;

            return ounces < 0.01f
                ? $"{grains:0.##} grains"
                : $"{ounces:0.####} oz";
        }
    }
}
