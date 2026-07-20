using UnityEditor;
using UnityEngine;

namespace Boomtown.WorldGeneration.Editor
{
    /// <summary>
    /// Scene-view visualization for generated Level 1 river hydrology data.
    /// Editor-only and does not modify RiverData or gameplay state.
    /// </summary>
    public sealed class RiverHydrologyDebugWindow : EditorWindow
    {
        private enum DisplayMode
        {
            Velocity,
            Slope,
            Bend,
            TransportCapacity,
            DepositionPotential,
            DepositedGold
        }

        private RiverData riverData;
        private DisplayMode displayMode = DisplayMode.Velocity;
        private bool showFlowArrows = true;
        private bool showSamplePoints = true;
        private float lineWidth = 5f;
        private float verticalOffset = 1.5f;

        [MenuItem("Boomtown/Debug/River Hydrology")]
        public static void Open()
        {
            GetWindow<RiverHydrologyDebugWindow>("River Hydrology");
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += DrawSceneOverlay;
            AutoDetectRiverData();
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= DrawSceneOverlay;
            SceneView.RepaintAll();
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "Editor/debug only. Colors are calculated from the generated RiverData asset.",
                MessageType.Info);

            riverData = (RiverData)EditorGUILayout.ObjectField(
                "River Data",
                riverData,
                typeof(RiverData),
                false);

            if (GUILayout.Button("Auto Detect Generated RiverData"))
            {
                AutoDetectRiverData();
            }

            EditorGUILayout.Space();
            displayMode = (DisplayMode)EditorGUILayout.EnumPopup(
                "Display",
                displayMode);
            showFlowArrows = EditorGUILayout.Toggle(
                "Show Flow Arrows",
                showFlowArrows);
            showSamplePoints = EditorGUILayout.Toggle(
                "Show Sample Points",
                showSamplePoints);
            lineWidth = EditorGUILayout.Slider(
                "Line Width",
                lineWidth,
                1f,
                12f);
            verticalOffset = EditorGUILayout.Slider(
                "Vertical Offset",
                verticalOffset,
                0f,
                10f);

            EditorGUILayout.Space();
            DrawLegend();

            if (GUI.changed)
            {
                SceneView.RepaintAll();
            }
        }

        private void AutoDetectRiverData()
        {
            string[] guids = AssetDatabase.FindAssets("t:RiverData");

            if (guids.Length == 0)
            {
                riverData = null;
                return;
            }

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            riverData = AssetDatabase.LoadAssetAtPath<RiverData>(path);
            Repaint();
            SceneView.RepaintAll();
        }

        private void DrawSceneOverlay(SceneView sceneView)
        {
            if (riverData == null ||
                riverData.samples == null ||
                riverData.samples.Count < 2)
            {
                return;
            }

            Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;

            for (int index = 0;
                 index < riverData.samples.Count - 1;
                 index++)
            {
                RiverSample current = riverData.samples[index];
                RiverSample next = riverData.samples[index + 1];

                Vector3 start = current.position + Vector3.up * verticalOffset;
                Vector3 end = next.position + Vector3.up * verticalOffset;

                Handles.color = GetSampleColor(current);
                Handles.DrawAAPolyLine(lineWidth, start, end);

                if (showSamplePoints)
                {
                    float size = HandleUtility.GetHandleSize(start) * 0.035f;
                    Handles.SphereHandleCap(
                        0,
                        start,
                        Quaternion.identity,
                        size,
                        EventType.Repaint);
                }

                if (showFlowArrows && index % 5 == 0)
                {
                    Vector3 direction = end - start;

                    if (direction.sqrMagnitude > 0.001f)
                    {
                        direction.Normalize();
                        float arrowSize =
                            HandleUtility.GetHandleSize(start) * 0.22f;

                        Handles.ArrowHandleCap(
                            0,
                            Vector3.Lerp(start, end, 0.5f),
                            Quaternion.LookRotation(direction),
                            arrowSize,
                            EventType.Repaint);
                    }
                }
            }
        }

        private Color GetSampleColor(RiverSample sample)
        {
            switch (displayMode)
            {
                case DisplayMode.Velocity:
                    return Color.Lerp(
                        new Color(0.10f, 0.45f, 1f),
                        Color.red,
                        Mathf.InverseLerp(0.25f, 4.5f, sample.velocity));

                case DisplayMode.Slope:
                    return Color.Lerp(
                        Color.cyan,
                        Color.magenta,
                        Mathf.InverseLerp(0f, 0.08f, sample.slope));

                case DisplayMode.Bend:
                    if (sample.bendType == RiverBendType.Left)
                    {
                        return Color.Lerp(Color.white, Color.green, sample.bendStrength);
                    }

                    if (sample.bendType == RiverBendType.Right)
                    {
                        return Color.Lerp(Color.white, new Color(1f, 0.45f, 0f), sample.bendStrength);
                    }

                    return Color.white;

                case DisplayMode.TransportCapacity:
                    return Color.Lerp(
                        new Color(0.25f, 0.20f, 0.10f),
                        Color.yellow,
                        sample.transportCapacity);

                case DisplayMode.DepositionPotential:
                    return Color.Lerp(
                        new Color(0.15f, 0.35f, 0.15f),
                        new Color(0.55f, 0.25f, 0.05f),
                        sample.depositionPotential);

                case DisplayMode.DepositedGold:
                    return Color.Lerp(
                        new Color(0.18f, 0.18f, 0.18f),
                        new Color(1f, 0.72f, 0.05f),
                        Mathf.Clamp01(sample.depositedGold));

                default:
                    return Color.white;
            }
        }

        private void DrawLegend()
        {
            string description;

            switch (displayMode)
            {
                case DisplayMode.Velocity:
                    description = "Blue = slow water   Red = fast water";
                    break;
                case DisplayMode.Slope:
                    description = "Cyan = flat   Magenta = steep";
                    break;
                case DisplayMode.Bend:
                    description = "White = straight   Green = left   Orange = right";
                    break;
                case DisplayMode.TransportCapacity:
                    description = "Dark = low transport   Yellow = high transport";
                    break;
                case DisplayMode.DepositionPotential:
                    description = "Green = low deposition   Brown = high deposition";
                    break;
                case DisplayMode.DepositedGold:
                    description = "Dark = little gold   Gold = strong placer deposit";
                    break;
                default:
                    description = string.Empty;
                    break;
            }

            EditorGUILayout.HelpBox(description, MessageType.None);
        }
    }
}
