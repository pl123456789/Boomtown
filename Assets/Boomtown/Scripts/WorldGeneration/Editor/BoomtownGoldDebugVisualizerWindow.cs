using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Boomtown.WorldGeneration.Editor
{
    public sealed class BoomtownGoldDebugVisualizerWindow : EditorWindow
    {
        private const string SourceRootName = "BT_GoldSourceOverlay";
        private const string FlowRootName = "BT_GoldFlowOverlay";
        private const string PlacerRootName = "BT_GoldRevealOverlay";

        private BoomtownGeologyData geologyData;
        private RiverData riverData;
        private Terrain targetTerrain;

        private GoldRevealMode placerMode = GoldRevealMode.Original;
        private float placerOpacity = 0.82f;
        private float placerIntensity = 4f;
        private int placerSpread = 5;
        private float placerMinimumAlpha = 0.18f;

        [MenuItem("Boomtown/Debug/Gold/Open Visualizer")]
        public static void Open()
        {
            BoomtownGoldDebugVisualizerWindow window =
                GetWindow<BoomtownGoldDebugVisualizerWindow>();

            window.titleContent = new GUIContent("Gold Debug");
            window.minSize = new Vector2(460f, 520f);
            window.AutoDetect();
        }

        [MenuItem("Boomtown/Debug/Gold/Show Mountains && Veins")]
        public static void MenuShowSources()
        {
            BoomtownGoldDebugVisualizerWindow window =
                GetWindow<BoomtownGoldDebugVisualizerWindow>();

            window.AutoDetect();
            window.ShowSources();
        }

        [MenuItem("Boomtown/Debug/Gold/Show Placer Heatmap")]
        public static void MenuShowPlacer()
        {
            BoomtownGoldDebugVisualizerWindow window =
                GetWindow<BoomtownGoldDebugVisualizerWindow>();

            window.AutoDetect();
            window.ShowPlacer();
        }

        [MenuItem("Boomtown/Debug/Gold/Show Flow Paths")]
        public static void MenuShowFlow()
        {
            BoomtownGoldDebugVisualizerWindow window =
                GetWindow<BoomtownGoldDebugVisualizerWindow>();

            window.AutoDetect();
            window.ShowFlowPaths();
        }

        [MenuItem("Boomtown/Debug/Gold/Hide All Overlays")]
        public static void HideAllOverlays()
        {
            DestroyByName(SourceRootName);
            DestroyByName(FlowRootName);
            DestroyByName(PlacerRootName);
            SceneView.RepaintAll();
        }

        private void OnEnable() => AutoDetect();

        private void OnFocus()
        {
            if (geologyData == null ||
                riverData == null ||
                targetTerrain == null)
            {
                AutoDetect();
            }
        }

        private void OnGUI()
        {
            GUIStyle title = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter
            };

            EditorGUILayout.LabelField("Gold Debug Visualizer", title);
            EditorGUILayout.Space(8f);
            EditorGUILayout.HelpBox(
                "Editor/debug only. Remove overlays before normal gameplay.",
                MessageType.Info);

            geologyData = (BoomtownGeologyData)EditorGUILayout.ObjectField(
                "Geology Data",
                geologyData,
                typeof(BoomtownGeologyData),
                false);

            riverData = (RiverData)EditorGUILayout.ObjectField(
                "River Data",
                riverData,
                typeof(RiverData),
                false);

            targetTerrain = (Terrain)EditorGUILayout.ObjectField(
                "Terrain",
                targetTerrain,
                typeof(Terrain),
                true);

            EditorGUILayout.Space(8f);

            if (GUILayout.Button("Auto Detect Generated Data", GUILayout.Height(30f)))
            {
                AutoDetect();
            }

            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("Hard-Rock Sources", EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(
                geologyData == null ||
                targetTerrain == null);

            if (GUILayout.Button(
                    "Show Mineralized Areas and Quartz Veins",
                    GUILayout.Height(38f)))
            {
                ShowSources();
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Placer Gold", EditorStyles.boldLabel);

            placerMode = (GoldRevealMode)EditorGUILayout.EnumPopup(
                "Heatmap Mode",
                placerMode);

            placerOpacity = EditorGUILayout.Slider(
                "Opacity",
                placerOpacity,
                0.05f,
                1f);

            placerIntensity = EditorGUILayout.Slider(
                "Intensity",
                placerIntensity,
                0.25f,
                12f);

            placerSpread = EditorGUILayout.IntSlider(
                "Visual Spread",
                placerSpread,
                0,
                10);

            placerMinimumAlpha = EditorGUILayout.Slider(
                "Minimum Alpha",
                placerMinimumAlpha,
                0f,
                0.6f);

            if (GUILayout.Button("Show Placer Heatmap", GUILayout.Height(38f)))
            {
                ShowPlacer();
            }

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Gold Movement", EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(
                geologyData == null ||
                riverData == null);

            if (GUILayout.Button("Show Approximate Flow Paths", GUILayout.Height(38f)))
            {
                ShowFlowPaths();
            }

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(12f);

            if (GUILayout.Button("Hide All Gold Overlays", GUILayout.Height(40f)))
            {
                HideAllOverlays();
            }

            EditorGUILayout.Space(10f);
            DrawStatistics();
        }

        private void AutoDetect()
        {
            targetTerrain =
                Terrain.activeTerrain ??
                Object.FindFirstObjectByType<Terrain>();

            geologyData = FindLatestAsset<BoomtownGeologyData>();
            riverData = FindLatestAsset<RiverData>();
            Repaint();
        }

        private void ShowSources()
        {
            if (geologyData == null ||
                targetTerrain == null)
            {
                Debug.LogError(
                    "[Gold Debug] Generated geology data or terrain is missing.");

                return;
            }

            DestroyByName(SourceRootName);
            GameObject root = new GameObject(SourceRootName);

            foreach (GoldMountainData mountain in geologyData.goldMountains)
            {
                CreateAlterationContours(root.transform, mountain);
            }

            foreach (QuartzVeinData vein in geologyData.quartzVeins)
            {
                CreateQuartzVein(root.transform, vein);
            }

            Selection.activeGameObject = root;
            SceneView.RepaintAll();

            Debug.Log(
                $"[Gold Debug] Showing terrain-following alteration contours for " +
                $"{geologyData.goldMountains.Count} mineralized mountain(s) and " +
                $"{geologyData.quartzVeins.Count} irregular quartz vein(s).");
        }

        private void CreateAlterationContours(
            Transform parent,
            GoldMountainData mountain)
        {
            const int contourCount = 3;
            const int pointCount = 48;

            for (int contourIndex = 0;
                 contourIndex < contourCount;
                 contourIndex++)
            {
                float contourT =
                    contourIndex /
                    (float)(contourCount - 1);

                float radiusScale = Mathf.Lerp(0.34f, 0.82f, contourT);

                GameObject contourObject = new GameObject(
                    $"{mountain.displayName} Alteration {contourIndex + 1}");

                contourObject.transform.SetParent(parent);

                LineRenderer line =
                    contourObject.AddComponent<LineRenderer>();

                line.loop = true;
                line.useWorldSpace = true;
                line.positionCount = pointCount;
                line.startWidth = Mathf.Lerp(4.5f, 1.8f, contourT);
                line.endWidth = line.startWidth;

                Color colour = new Color(
                    0.62f,
                    0.24f,
                    0.08f,
                    Mathf.Lerp(0.42f, 0.16f, contourT));

                line.startColor = colour;
                line.endColor = colour;
                line.sharedMaterial = CreateTransparentMaterial(Color.white);

                for (int pointIndex = 0;
                     pointIndex < pointCount;
                     pointIndex++)
                {
                    float angle =
                        pointIndex /
                        (float)pointCount *
                        Mathf.PI *
                        2f;

                    float irregularity =
                        1f +
                        Mathf.Sin(angle * 3f + mountain.id * 1.73f) * 0.11f +
                        Mathf.Sin(angle * 7f + mountain.id * 0.91f) * 0.05f;

                    float radius =
                        mountain.radius *
                        radiusScale *
                        irregularity;

                    Vector3 point =
                        mountain.centre +
                        new Vector3(
                            Mathf.Cos(angle) * radius,
                            0f,
                            Mathf.Sin(angle) * radius);

                    line.SetPosition(
                        pointIndex,
                        ConformToTerrain(
                            point,
                            2.5f + contourIndex * 0.4f));
                }
            }
        }

        private void CreateQuartzVein(
            Transform parent,
            QuartzVeinData vein)
        {
            const int pointCount = 11;

            GameObject lineObject =
                new GameObject($"Quartz Vein {vein.id}");

            lineObject.transform.SetParent(parent);

            LineRenderer line =
                lineObject.AddComponent<LineRenderer>();

            line.useWorldSpace = true;
            line.positionCount = pointCount;

            float grade = Mathf.InverseLerp(
                0.04f,
                0.95f,
                vein.gradeOuncesPerTon);

            line.startWidth =
                Mathf.Max(1.4f, vein.widthMetres * 2.2f);

            line.endWidth =
                Mathf.Max(0.8f, line.startWidth * 0.58f);

            Color quartzColour = Color.Lerp(
                new Color(0.88f, 0.90f, 0.92f, 0.90f),
                new Color(1f, 0.82f, 0.24f, 0.96f),
                grade * 0.42f);

            line.startColor = quartzColour;
            line.endColor = quartzColour;
            line.sharedMaterial = CreateTransparentMaterial(Color.white);

            Vector3 direction = vein.end - vein.start;
            Vector3 sideways =
                Vector3.Cross(
                    Vector3.up,
                    direction.normalized);

            float length = direction.magnitude;

            for (int pointIndex = 0;
                 pointIndex < pointCount;
                 pointIndex++)
            {
                float t =
                    pointIndex /
                    (float)(pointCount - 1);

                Vector3 point =
                    Vector3.Lerp(
                        vein.start,
                        vein.end,
                        t);

                float wave =
                    Mathf.Sin(
                        t * Mathf.PI * 3.2f +
                        vein.id * 1.37f) *
                    Mathf.Lerp(
                        4f,
                        15f,
                        Mathf.Clamp01(length / 620f));

                float secondary =
                    Mathf.Sin(
                        t * Mathf.PI * 7.4f +
                        vein.id * 0.63f) *
                    2.5f;

                point += sideways * (wave + secondary);

                line.SetPosition(
                    pointIndex,
                    ConformToTerrain(point, 3.5f));
            }
        }

        private void ShowPlacer()
        {
            if (geologyData == null ||
                targetTerrain == null)
            {
                Debug.LogError(
                    "[Gold Debug] Geology data or terrain is missing.");

                return;
            }

            GameObject overlayObject =
                GameObject.Find(PlacerRootName);

            if (overlayObject == null)
            {
                overlayObject =
                    new GameObject(PlacerRootName);

                Undo.RegisterCreatedObjectUndo(
                    overlayObject,
                    "Create Gold Placer Overlay");
            }

            BoomtownGoldRevealOverlay overlay =
                overlayObject.GetComponent<BoomtownGoldRevealOverlay>();

            if (overlay == null)
            {
                overlay =
                    Undo.AddComponent<BoomtownGoldRevealOverlay>(
                        overlayObject);
            }

            overlay.Configure(
                geologyData,
                targetTerrain,
                GetOrCreatePlacerMaterial(),
                placerMode,
                placerOpacity,
                placerIntensity,
                placerSpread,
                placerMinimumAlpha);

            Selection.activeGameObject = overlayObject;
            SceneView.RepaintAll();
        }

        private void ShowFlowPaths()
        {
            if (geologyData == null ||
                riverData == null ||
                riverData.samples == null ||
                riverData.samples.Count == 0)
            {
                Debug.LogError(
                    "[Gold Debug] Geology or RiverData is missing.");

                return;
            }

            DestroyByName(FlowRootName);
            GameObject root = new GameObject(FlowRootName);

            foreach (QuartzVeinData vein in geologyData.quartzVeins)
            {
                Vector3 veinCentre =
                    Vector3.Lerp(
                        vein.start,
                        vein.end,
                        0.5f);

                List<Vector3> points =
                    BuildFlowPath(
                        vein,
                        veinCentre);

                GameObject pathObject =
                    new GameObject(
                        $"Gold Flow from Vein {vein.id}");

                pathObject.transform.SetParent(root.transform);

                LineRenderer line =
                    pathObject.AddComponent<LineRenderer>();

                line.useWorldSpace = true;
                line.positionCount = points.Count;
                line.SetPositions(points.ToArray());
                line.startWidth = 3f;
                line.endWidth = 1.2f;
                line.startColor = new Color(0.95f, 0.62f, 0.08f, 0.72f);
                line.endColor = new Color(0.85f, 0.30f, 0.04f, 0.28f);
                line.sharedMaterial = CreateTransparentMaterial(Color.white);
            }

            Selection.activeGameObject = root;
            SceneView.RepaintAll();

            Debug.Log(
                $"[Gold Debug] Showing " +
                $"{geologyData.quartzVeins.Count} approximate flow path(s).");
        }

        private List<Vector3> BuildFlowPath(
            QuartzVeinData vein,
            Vector3 veinCentre)
        {
            List<Vector3> points = new List<Vector3>();

            Vector3 source =
                ConformToTerrain(
                    veinCentre,
                    6f);

            Vector3 riverEntry =
                vein.riverEntryPosition;

            if (riverEntry == Vector3.zero)
            {
                riverEntry =
                    riverData.samples[
                        FindNearestRiverSampleIndex(
                            veinCentre)].position;
            }

            const int approachPoints = 10;

            for (int index = 0;
                 index < approachPoints;
                 index++)
            {
                float t =
                    index /
                    (float)(approachPoints - 1);

                Vector3 point =
                    Vector3.Lerp(
                        source,
                        riverEntry,
                        t);

                Vector3 direction =
                    riverEntry - source;

                Vector3 right =
                    Vector3.Cross(
                        Vector3.up,
                        direction.normalized);

                point +=
                    right *
                    Mathf.Sin(t * Mathf.PI * 2.1f) *
                    12f *
                    (1f - t);

                points.Add(
                    ConformToTerrain(
                        point,
                        5f));
            }

            int entryIndex = Mathf.Clamp(
                vein.riverEntrySampleIndex,
                0,
                riverData.samples.Count - 1);

            int downstreamEnd = Mathf.Min(
                riverData.samples.Count - 1,
                entryIndex + 30);

            for (int sampleIndex = entryIndex;
                 sampleIndex <= downstreamEnd;
                 sampleIndex += 2)
            {
                points.Add(
                    riverData.samples[sampleIndex].position +
                    Vector3.up * 5f);
            }

            return points;
        }

        private int FindNearestRiverSampleIndex(
            Vector3 position)
        {
            int nearestIndex = 0;
            float bestDistance = float.MaxValue;

            for (int index = 0;
                 index < riverData.samples.Count;
                 index++)
            {
                Vector3 delta =
                    riverData.samples[index].position -
                    position;

                delta.y = 0f;

                float distance = delta.sqrMagnitude;

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    nearestIndex = index;
                }
            }

            return nearestIndex;
        }

        private Vector3 ConformToTerrain(
            Vector3 point,
            float offset)
        {
            if (targetTerrain == null)
            {
                point.y += offset;
                return point;
            }

            point.y =
                targetTerrain.SampleHeight(point) +
                targetTerrain.transform.position.y +
                offset;

            return point;
        }

        private void DrawStatistics()
        {
            if (geologyData == null)
            {
                return;
            }

            EditorGUILayout.LabelField(
                "Generated Gold Statistics",
                EditorStyles.boldLabel);

            EditorGUILayout.LabelField(
                "Mineralized mountains",
                geologyData.goldMountains?.Count.ToString() ?? "0");

            EditorGUILayout.LabelField(
                "Quartz veins",
                geologyData.quartzVeins?.Count.ToString() ?? "0");

            EditorGUILayout.LabelField(
                "Initial hard-rock gold",
                $"{geologyData.GetTotalInitialHardRockOunces():0.00} oz");

            EditorGUILayout.LabelField(
                "Initial placer gold",
                $"{geologyData.GetTotalInitialOunces():0.00} oz");

            EditorGUILayout.LabelField(
                "Remaining placer gold",
                $"{geologyData.GetTotalRemainingOunces():0.00} oz");

            EditorGUILayout.LabelField(
                "Sediment layers",
                geologyData.HasSedimentLayers
                    ? "Sand / Gravel / Black Sand"
                    : "Not generated");
        }

        private static T FindLatestAsset<T>()
            where T : Object
        {
            string[] guids = AssetDatabase.FindAssets(
                $"t:{typeof(T).Name}",
                new[]
                {
                    "Assets/Boomtown/Scripts/WorldGeneration/Generated"
                });

            if (guids.Length == 0)
            {
                return null;
            }

            string newestPath = null;
            long newestTicks = long.MinValue;

            foreach (string guid in guids)
            {
                string path =
                    AssetDatabase.GUIDToAssetPath(guid);

                long ticks =
                    System.IO.File.Exists(path)
                        ? System.IO.File.GetLastWriteTimeUtc(path).Ticks
                        : 0L;

                if (ticks > newestTicks)
                {
                    newestTicks = ticks;
                    newestPath = path;
                }
            }

            return string.IsNullOrEmpty(newestPath)
                ? null
                : AssetDatabase.LoadAssetAtPath<T>(newestPath);
        }

        private static Material CreateTransparentMaterial(
            Color colour)
        {
            Shader shader =
                Shader.Find("Universal Render Pipeline/Unlit") ??
                Shader.Find("Sprites/Default");

            Material material =
                new Material(shader);

            material.color = colour;

            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
            }

            if (material.HasProperty("_ZWrite"))
            {
                material.SetFloat("_ZWrite", 0f);
            }

            material.renderQueue = 3000;
            return material;
        }

        private static Material GetOrCreatePlacerMaterial()
        {
            const string materialPath =
                "Assets/Boomtown/Scripts/WorldGeneration/Generated/" +
                "BT_GoldRevealOverlay.mat";

            Material material =
                AssetDatabase.LoadAssetAtPath<Material>(materialPath);

            Shader shader =
                Shader.Find("Boomtown/Gold Reveal Overlay");

            if (shader == null)
            {
                Debug.LogError(
                    "[Gold Debug] Gold reveal shader not found.");

                return null;
            }

            if (material == null)
            {
                material = new Material(shader)
                {
                    name = "BT_GoldRevealOverlay"
                };

                AssetDatabase.CreateAsset(
                    material,
                    materialPath);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }

        private static void DestroyByName(
            string objectName)
        {
            GameObject target =
                GameObject.Find(objectName);

            if (target != null)
            {
                Undo.DestroyObjectImmediate(target);
            }
        }
    }
}
