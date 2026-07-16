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

            Keyboard keyboard =
                Keyboard.current;

            bool shiftPressed =
                keyboard != null &&
                (keyboard.leftShiftKey.isPressed ||
                 keyboard.rightShiftKey.isPressed);

            if (sensor.CanPanHere &&
                keyboard != null &&
                !shiftPressed &&
                keyboard[panKey].wasPressedThisFrame)
            {
                TryStartPanning();
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

            // Let Unity rebuild the newly enabled Slider before animation starts.
            yield return null;

            float elapsed = 0f;

            while (elapsed < panningDuration)
            {
                elapsed += Time.deltaTime;

                float progress =
                    Mathf.Clamp01(
                        elapsed / panningDuration);

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

            float geologyGrade =
                geologyData.SamplePlacer(
                    sensor.SamplePoint);

            PanOutcome outcome =
                resultEvaluator.Evaluate(
                    geologyGrade);

            inventory.AddGold(
                outcome.GoldOunces);

            string resultText =
                BuildResultText(
                    outcome,
                    inventory.TotalGoldOunces);

            if (showPlayerUI)
            {
                ui.ShowResult(resultText);
            }

            Debug.Log(
                $"[Gold Panning] {resultText} | " +
                $"Geology grade: {outcome.SampledGrade:0.000}",
                this);

            if (resultDisplayDuration > 0f)
            {
                yield return new WaitForSeconds(
                    resultDisplayDuration);
            }

            RestoreMovement();

            isPanning = false;

            Action completion =
                queuedCompletion;

            queuedCompletion = null;

            completion?.Invoke();

            UpdatePrompt();
        }

        private void UpdatePrompt()
        {
            if (!showPlayerUI ||
                ui == null)
            {
                return;
            }

            if (geologyData == null ||
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
                agent.isStopped = false;
            }
        }

        private static string BuildResultText(
            PanOutcome outcome,
            float totalGoldOunces)
        {
            string reward =
                outcome.GoldOunces > 0f
                    ? $"+{FormatGold(outcome.GoldOunces)}"
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
