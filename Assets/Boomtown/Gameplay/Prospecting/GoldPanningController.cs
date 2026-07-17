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
        private enum PanningUiState
        {
            Hidden,
            Prompt,
            Progress,
            Result
        }

        private const float GrainsPerTroyOunce = 480f;

        [Header("Generated Geology")]
        [SerializeField] private BoomtownGeologyData geologyData;

        [Header("Panning")]
        [SerializeField, Min(0.1f)] private float panningDuration = 4f;
        [SerializeField, Min(0f)] private float resultDisplayDuration = 2.5f;
        [SerializeField, Min(0f)] private float completionHoldDuration = 0.12f;
        [SerializeField, Min(0f)] private float turnSmoothing = 9f;
        [SerializeField] private AnimationCurve progressCurve =
            AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private Key panKey = Key.Space;
        [SerializeField] private PanResultEvaluator resultEvaluator = new();

        [Header("UI")]
        [Tooltip("Allows this character to use the shared gameplay panning UI when selected.")]
        [SerializeField] private bool showPlayerUI = true;
        [SerializeField] private GoldPanningUI ui;

        private NavMeshAgent agent;
        private ProspectingLocationSensor sensor;
        private GoldInventory inventory;
        private readonly List<Behaviour> disabledMovementBehaviours = new();

        private bool isPanning;
        private bool playerUiActive;
        private Action queuedCompletion;
        private PanningUiState uiState;
        private float currentProgress;
        private string currentResult;

        public bool IsPanning => isPanning;
        public bool IsPlayerUiActive => playerUiActive;

        public void AssignGeologyData(BoomtownGeologyData generatedGeology)
        {
            geologyData = generatedGeology;
        }

        public bool CanPanAt(Vector3 worldPosition)
        {
            return sensor != null && sensor.CanPanAt(worldPosition);
        }

        public void SetPlayerUIActive(bool active)
        {
            playerUiActive = showPlayerUI && active;

            if (ui == null)
            {
                return;
            }

            if (!playerUiActive)
            {
                ui.HideAll();
                return;
            }

            RefreshPlayerUI();
        }

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            sensor = GetComponent<ProspectingLocationSensor>();
            inventory = GetComponent<GoldInventory>();
            uiState = PanningUiState.Hidden;
        }

        private void Start()
        {
            if (geologyData == null)
            {
                Debug.LogError("[Gold Panning] BoomtownGeologyData is not assigned.", this);
            }

            if (showPlayerUI && ui == null)
            {
                Debug.LogError("[Gold Panning] GoldPanningUI is not assigned.", this);
            }
        }

        private void Update()
        {
            if (isPanning)
            {
                return;
            }

            UpdatePromptState();

            if (!playerUiActive)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            bool shiftPressed = keyboard != null &&
                (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);

            if (sensor != null && sensor.CanPanHere && keyboard != null &&
                !shiftPressed && keyboard[panKey].wasPressedThisFrame)
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

            StopAllCoroutines();
            RestoreMovement();
            isPanning = false;
            queuedCompletion = null;
            uiState = PanningUiState.Hidden;

            if (playerUiActive && ui != null)
            {
                ui.HideAll();
            }
        }

        public bool TryStartPanning()
        {
            return TryStartPanning(null);
        }

        public bool TryStartPanning(Action onCompleted)
        {
            sensor?.Refresh();

            if (isPanning || geologyData == null || sensor == null ||
                inventory == null || !sensor.CanPanHere)
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
            currentProgress = 0f;
            currentResult = string.Empty;
            uiState = PanningUiState.Progress;
            StopAndLockMovement();
            RefreshPlayerUI();

            yield return null;

            float elapsed = 0f;
            while (elapsed < panningDuration)
            {
                elapsed += Time.deltaTime;
                float normalized = Mathf.Clamp01(elapsed / panningDuration);
                currentProgress = progressCurve != null
                    ? Mathf.Clamp01(progressCurve.Evaluate(normalized))
                    : normalized;

                RotateTowardSamplePoint();

                if (playerUiActive && ui != null)
                {
                    ui.ShowProgress(currentProgress);
                }

                yield return null;
            }

            currentProgress = 1f;
            RefreshPlayerUI();

            if (completionHoldDuration > 0f)
            {
                yield return new WaitForSeconds(completionHoldDuration);
            }

            GoldExtractionResult extraction = BoomtownGoldExtractionService.Pan(
                geologyData,
                sensor.SamplePoint,
                materialProcessed: 1f,
                recoveryEfficiency: 0.65f);

            PanOutcome outcome = resultEvaluator.Evaluate(extraction.grade);
            inventory.AddGold(extraction.extractedOunces);
            currentResult = BuildResultText(
                outcome,
                extraction.extractedOunces,
                inventory.TotalGoldOunces);

            uiState = PanningUiState.Result;
            RefreshPlayerUI();

            Debug.Log(
                $"[Gold Panning] {currentResult} | " +
                $"Geology grade: {extraction.grade:0.000} | " +
                $"Deposit remaining: {extraction.remainingOunces:0.####} oz",
                this);

            if (resultDisplayDuration > 0f)
            {
                yield return new WaitForSeconds(resultDisplayDuration);
            }

            RestoreMovement();
            isPanning = false;

            Action completion = queuedCompletion;
            queuedCompletion = null;
            completion?.Invoke();

            UpdatePromptState();
        }

        private void UpdatePromptState()
        {
            if (isPanning)
            {
                return;
            }

            sensor?.Refresh();
            uiState = geologyData != null && sensor != null && sensor.CanPanHere
                ? PanningUiState.Prompt
                : PanningUiState.Hidden;

            RefreshPlayerUI();
        }

        private void RefreshPlayerUI()
        {
            if (!playerUiActive || ui == null)
            {
                return;
            }

            switch (uiState)
            {
                case PanningUiState.Prompt:
                    ui.ShowPrompt($"Press {panKey} to Pan for Gold");
                    break;
                case PanningUiState.Progress:
                    ui.BeginProgress(currentProgress);
                    break;
                case PanningUiState.Result:
                    ui.ShowResult(currentResult);
                    break;
                default:
                    ui.HideAll();
                    break;
            }
        }

        private void RotateTowardSamplePoint()
        {
            if (sensor == null || turnSmoothing <= 0f)
            {
                return;
            }

            Vector3 direction = sensor.SamplePoint - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            float smoothing = 1f - Mathf.Exp(-turnSmoothing * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, smoothing);
        }

        private void StopAndLockMovement()
        {
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.ResetPath();
                agent.velocity = Vector3.zero;
                agent.isStopped = true;
            }

            disabledMovementBehaviours.Clear();
            foreach (MonoBehaviour behaviour in GetComponents<MonoBehaviour>())
            {
                if (behaviour == null || behaviour == this || behaviour == sensor || behaviour == inventory)
                {
                    continue;
                }

                string typeName = behaviour.GetType().Name;
                bool isMovementBehaviour =
                    typeName == "QuickPlayerController" ||
                    typeName == "EmployeeMovement" ||
                    typeName == "WaypointPath";

                if (isMovementBehaviour && behaviour.enabled)
                {
                    behaviour.enabled = false;
                    disabledMovementBehaviours.Add(behaviour);
                }
            }
        }

        private void RestoreMovement()
        {
            foreach (Behaviour behaviour in disabledMovementBehaviours)
            {
                if (behaviour != null)
                {
                    behaviour.enabled = true;
                }
            }

            disabledMovementBehaviours.Clear();

            if (agent != null && agent.enabled && agent.isOnNavMesh)
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
            string reward = extractedOunces > 0f
                ? $"+{FormatGold(extractedOunces)}"
                : "No gold";

            return $"{outcome.Description}  {reward}\n" +
                   $"Total Gold: {FormatGold(totalGoldOunces)}";
        }

        private static string FormatGold(float ounces)
        {
            float grains = ounces * GrainsPerTroyOunce;
            return ounces < 0.01f
                ? $"{grains:0.##} grains"
                : $"{ounces:0.####} oz";
        }
    }
}
