using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Boomtown.WorldGeneration.Editor
{
    public sealed class BoomtownGoldDebugVisualizerWindow :
        EditorWindow
    {
        private const string SourceRootName =
            "BT_GoldSourceOverlay";

        private const string FlowRootName =
            "BT_GoldFlowOverlay";

        private const string PlacerRootName =
            "BT_GoldRevealOverlay";

        private BoomtownGeologyData geologyData;
        private RiverData riverData;
        private Terrain targetTerrain;

        private GoldRevealMode placerMode =
            GoldRevealMode.Original;

        private float placerOpacity = 0.82f;
        private float placerIntensity = 4f;
        private int placerSpread = 5;
        private float placerMinimumAlpha = 0.18f;

        [MenuItem(
            "Boomtown/Debug/Gold/Open Visualizer")]
        public static void Open()
        {
            BoomtownGoldDebugVisualizerWindow window =
                GetWindow<
                    BoomtownGoldDebugVisualizerWindow>();

            window.titleContent =
                new GUIContent(
                    "Gold Debug");

            window.minSize =
                new Vector2(
                    460f,
                    520f);

            window.AutoDetect();
        }

        [MenuItem(
            "Boomtown/Debug/Gold/Show Mountains && Veins")]
        public static void MenuShowSources()
        {
            BoomtownGoldDebugVisualizerWindow window =
                GetWindow<
                    BoomtownGoldDebugVisualizerWindow>();

            window.AutoDetect();
            window.ShowSources();
        }

        [MenuItem(
            "Boomtown/Debug/Gold/Show Placer Heatmap")]
        public static void MenuShowPlacer()
        {
            BoomtownGoldDebugVisualizerWindow window =
                GetWindow<
                    BoomtownGoldDebugVisualizerWindow>();

            window.AutoDetect();
            window.ShowPlacer();
        }

        [MenuItem(
            "Boomtown/Debug/Gold/Show Flow Paths")]
        public static void MenuShowFlow()
        {
            BoomtownGoldDebugVisualizerWindow window =
                GetWindow<
                    BoomtownGoldDebugVisualizerWindow>();

            window.AutoDetect();
            window.ShowFlowPaths();
        }

        [MenuItem(
            "Boomtown/Debug/Gold/Hide All Overlays")]
        public static void HideAllOverlays()
        {
            DestroyByName(
                SourceRootName);

            DestroyByName(
                FlowRootName);

            DestroyByName(
                PlacerRootName);

            SceneView.RepaintAll();
        }

        private void OnEnable()
        {
            AutoDetect();
        }

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
            GUIStyle title =
                new GUIStyle(
                    EditorStyles.boldLabel)
                {
                    fontSize = 18,
                    alignment =
                        TextAnchor.MiddleCenter
                };

            EditorGUILayout.LabelField(
                "Gold Debug Visualizer",
                title);

            EditorGUILayout.Space(8f);

            EditorGUILayout.HelpBox(
                "Editor/debug only. These overlays reveal hidden geology and " +
                "should be removed before normal gameplay.",
                MessageType.Info);

            EditorGUILayout.Space(8f);

            geologyData =
                (BoomtownGeologyData)
                EditorGUILayout.ObjectField(
                    "Geology Data",
                    geologyData,
                    typeof(BoomtownGeologyData),
                    false);

            riverData =
                (RiverData)
                EditorGUILayout.ObjectField(
                    "River Data",
                    riverData,
                    typeof(RiverData),
                    false);

            targetTerrain =
                (Terrain)
                EditorGUILayout.ObjectField(
                    "Terrain",
                    targetTerrain,
                    typeof(Terrain),
                    true);

            EditorGUILayout.Space(10f);

            if (GUILayout.Button(
                    "Auto Detect Generated Data",
                    GUILayout.Height(32f)))
            {
                AutoDetect();
            }

            EditorGUILayout.Space(12f);

            EditorGUILayout.LabelField(
                "Hard-Rock Sources",
                EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(
                geologyData == null);

            if (GUILayout.Button(
                    "Show Gold Mountains and Veins",
                    GUILayout.Height(38f)))
            {
                ShowSources();
            }

            EditorGUILayout.Space(8f);

            EditorGUILayout.LabelField(
                "Placer Gold",
                EditorStyles.boldLabel);

            placerMode =
                (GoldRevealMode)
                EditorGUILayout.EnumPopup(
                    "Heatmap Mode",
                    placerMode);

            placerOpacity =
                EditorGUILayout.Slider(
                    "Opacity",
                    placerOpacity,
                    0.05f,
                    1f);

            placerIntensity =
                EditorGUILayout.Slider(
                    "Intensity",
                    placerIntensity,
                    0.25f,
                    12f);

            placerSpread =
                EditorGUILayout.IntSlider(
                    "Visual Spread",
                    placerSpread,
                    0,
                    10);

            placerMinimumAlpha =
                EditorGUILayout.Slider(
                    "Minimum Alpha",
                    placerMinimumAlpha,
                    0f,
                    0.6f);

            if (GUILayout.Button(
                    "Show Placer Heatmap",
                    GUILayout.Height(38f)))
            {
                ShowPlacer();
            }

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(8f);

            EditorGUILayout.LabelField(
                "Gold Movement",
                EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(
                geologyData == null ||
                riverData == null);

            if (GUILayout.Button(
                    "Show Approximate Gold Flow Paths",
                    GUILayout.Height(38f)))
            {
                ShowFlowPaths();
            }

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(14f);

            if (GUILayout.Button(
                    "Hide All Gold Overlays",
                    GUILayout.Height(40f)))
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
                Object.FindFirstObjectByType<
                    Terrain>();

            geologyData =
                FindLatestAsset<
                    BoomtownGeologyData>();

            riverData =
                FindLatestAsset<
                    RiverData>();

            Repaint();
        }

        private void ShowSources()
        {
            if (geologyData == null)
            {
                Debug.LogError(
                    "[Gold Debug] No generated geology data found.");

                return;
            }

            DestroyByName(
                SourceRootName);

            GameObject root =
                new GameObject(
                    SourceRootName);

            foreach (GoldMountainData mountain
                     in geologyData.goldMountains)
            {
                GameObject marker =
                    GameObject.CreatePrimitive(
                        PrimitiveType.Sphere);

                marker.name =
                    mountain.displayName;

                marker.transform.SetParent(
                    root.transform);

                marker.transform.position =
                    mountain.centre +
                    Vector3.up * 45f;

                marker.transform.localScale =
                    new Vector3(
                        mountain.radius * 2f,
                        90f,
                        mountain.radius * 2f);

                DestroyImmediate(
                    marker.GetComponent<Collider>());

                Renderer renderer =
                    marker.GetComponent<Renderer>();

                renderer.sharedMaterial =
                    CreateTransparentMaterial(
                        new Color(
                            1f,
                            0.45f,
                            0f,
                            0.17f));
            }

            foreach (QuartzVeinData vein
                     in geologyData.quartzVeins)
            {
                GameObject lineObject =
                    new GameObject(
                        $"Quartz Vein {vein.id}");

                lineObject.transform.SetParent(
                    root.transform);

                LineRenderer line =
                    lineObject.AddComponent<
                        LineRenderer>();

                line.positionCount = 2;

                line.SetPosition(
                    0,
                    vein.start +
                    Vector3.up * 4f);

                line.SetPosition(
                    1,
                    vein.end +
                    Vector3.up * 4f);

                line.startWidth =
                    Mathf.Max(
                        2f,
                        vein.widthMetres * 4f);

                line.endWidth =
                    line.startWidth;

                float grade =
                    Mathf.InverseLerp(
                        0.04f,
                        0.95f,
                        vein.gradeOuncesPerTon);

                Color colour =
                    Color.Lerp(
                        Color.white,
                        new Color(
                            1f,
                            0.68f,
                            0.02f,
                            1f),
                        grade);

                line.startColor =
                    colour;

                line.endColor =
                    colour;

                line.material =
                    new Material(
                        Shader.Find(
                            "Sprites/Default"));
            }

            Selection.activeGameObject =
                root;

            SceneView.RepaintAll();

            Debug.Log(
                $"[Gold Debug] Showing " +
                $"{geologyData.goldMountains.Count} mineralized mountain(s) " +
                $"and {geologyData.quartzVeins.Count} quartz vein(s).");
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
                GameObject.Find(
                    PlacerRootName);

            if (overlayObject == null)
            {
                overlayObject =
                    new GameObject(
                        PlacerRootName);

                Undo.RegisterCreatedObjectUndo(
                    overlayObject,
                    "Create Gold Placer Overlay");
            }

            BoomtownGoldRevealOverlay overlay =
                overlayObject.GetComponent<
                    BoomtownGoldRevealOverlay>();

            if (overlay == null)
            {
                overlay =
                    Undo.AddComponent<
                        BoomtownGoldRevealOverlay>(
                        overlayObject);
            }

            Material material =
                GetOrCreatePlacerMaterial();

            overlay.Configure(
                geologyData,
                targetTerrain,
                material,
                placerMode,
                placerOpacity,
                placerIntensity,
                placerSpread,
                placerMinimumAlpha);

            Selection.activeGameObject =
                overlayObject;

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

            DestroyByName(
                FlowRootName);

            GameObject root =
                new GameObject(
                    FlowRootName);

            foreach (QuartzVeinData vein
                     in geologyData.quartzVeins)
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

                pathObject.transform.SetParent(
                    root.transform);

                LineRenderer line =
                    pathObject.AddComponent<
                        LineRenderer>();

                line.positionCount =
                    points.Count;

                line.SetPositions(
                    points.ToArray());

                line.startWidth = 5f;
                line.endWidth = 2f;

                line.startColor =
                    new Color(
                        1f,
                        0.78f,
                        0.02f,
                        0.95f);

                line.endColor =
                    new Color(
                        1f,
                        0.30f,
                        0.02f,
                        0.65f);

                line.material =
                    new Material(
                        Shader.Find(
                            "Sprites/Default"));
            }

            Selection.activeGameObject =
                root;

            SceneView.RepaintAll();

            Debug.Log(
                $"[Gold Debug] Showing " +
                $"{geologyData.quartzVeins.Count} approximate flow path(s). " +
                "These are visual diagnostics until full watershed transport " +
                "is implemented.");
        }

        private List<Vector3> BuildFlowPath(
            QuartzVeinData vein,
            Vector3 veinCentre)
        {
            List<Vector3> points =
                new List<Vector3>();

            Vector3 source =
                ConformToTerrain(
                    veinCentre,
                    8f);

            Vector3 riverEntry =
                vein.riverEntryPosition;

            if (riverEntry == Vector3.zero)
            {
                int nearestIndex =
                    FindNearestRiverSampleIndex(
                        veinCentre);

                riverEntry =
                    riverData.samples[
                        nearestIndex].position;
            }

            const int approachPoints = 10;

            for (int index = 0;
                 index < approachPoints;
                 index++)
            {
                float t =
                    index /
                    (float)(
                        approachPoints - 1);

                Vector3 point =
                    Vector3.Lerp(
                        source,
                        riverEntry,
                        t);

                Vector3 direction =
                    riverEntry -
                    source;

                Vector3 right =
                    Vector3.Cross(
                        Vector3.up,
                        direction.normalized);

                point +=
                    right *
                    Mathf.Sin(
                        t *
                        Mathf.PI *
                        2.1f) *
                    14f *
                    (1f - t);

                points.Add(
                    ConformToTerrain(
                        point,
                        7f));
            }

            int entryIndex =
                Mathf.Clamp(
                    vein.riverEntrySampleIndex,
                    0,
                    riverData.samples.Count - 1);

            int downstreamEnd =
                Mathf.Min(
                    riverData.samples.Count - 1,
                    entryIndex + 30);

            for (int sampleIndex =
                     entryIndex;
                 sampleIndex <=
                 downstreamEnd;
                 sampleIndex += 2)
            {
                points.Add(
                    riverData.samples[
                        sampleIndex].position +
                    Vector3.up * 7f);
            }

            return points;
        }

        private int FindNearestRiverSampleIndex(
            Vector3 position)
        {
            int nearestIndex = 0;
            float bestDistance =
                float.MaxValue;

            for (int index = 0;
                 index <
                 riverData.samples.Count;
                 index++)
            {
                Vector3 delta =
                    riverData.samples[index]
                        .position -
                    position;

                delta.y = 0f;

                float distance =
                    delta.sqrMagnitude;

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
                targetTerrain.SampleHeight(
                    point) +
                targetTerrain.transform
                    .position.y +
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
                geologyData.goldMountains?.Count
                    .ToString() ??
                "0");

            EditorGUILayout.LabelField(
                "Quartz veins",
                geologyData.quartzVeins?.Count
                    .ToString() ??
                "0");

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
            string[] guids =
                AssetDatabase.FindAssets(
                    $"t:{typeof(T).Name}",
                    new[]
                    {
                        "Assets/Boomtown/Scripts/" +
                        "WorldGeneration/Generated"
                    });

            if (guids.Length == 0)
            {
                return null;
            }

            string newestPath = null;
            long newestTicks =
                long.MinValue;

            foreach (string guid in guids)
            {
                string path =
                    AssetDatabase.GUIDToAssetPath(
                        guid);

                long ticks =
                    System.IO.File.Exists(
                        path)
                        ? System.IO.File
                            .GetLastWriteTimeUtc(
                                path)
                            .Ticks
                        : 0L;

                if (ticks > newestTicks)
                {
                    newestTicks = ticks;
                    newestPath = path;
                }
            }

            return string.IsNullOrEmpty(
                    newestPath)
                ? null
                : AssetDatabase
                    .LoadAssetAtPath<T>(
                        newestPath);
        }

        private static Material
            CreateTransparentMaterial(
                Color colour)
        {
            Shader shader =
                Shader.Find(
                    "Universal Render Pipeline/Unlit");

            if (shader == null)
            {
                shader =
                    Shader.Find(
                        "Sprites/Default");
            }

            Material material =
                new Material(shader);

            material.color =
                colour;

            if (material.HasProperty(
                    "_Surface"))
            {
                material.SetFloat(
                    "_Surface",
                    1f);
            }

            material.renderQueue =
                3000;

            return material;
        }

        private static Material
            GetOrCreatePlacerMaterial()
        {
            const string materialPath =
                "Assets/Boomtown/Scripts/" +
                "WorldGeneration/Generated/" +
                "BT_GoldRevealOverlay.mat";

            Material material =
                AssetDatabase
                    .LoadAssetAtPath<Material>(
                        materialPath);

            Shader shader =
                Shader.Find(
                    "Boomtown/Gold Reveal Overlay");

            if (shader == null)
            {
                Debug.LogError(
                    "[Gold Debug] Gold reveal shader not found.");

                return null;
            }

            if (material == null)
            {
                material =
                    new Material(shader)
                    {
                        name =
                            "BT_GoldRevealOverlay"
                    };

                AssetDatabase.CreateAsset(
                    material,
                    materialPath);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            EditorUtility.SetDirty(
                material);

            AssetDatabase.SaveAssets();

            return material;
        }

        private static void DestroyByName(
            string objectName)
        {
            GameObject target =
                GameObject.Find(
                    objectName);

            if (target != null)
            {
                Undo.DestroyObjectImmediate(
                    target);
            }
        }
    }
}
