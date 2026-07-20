using UnityEditor;
using UnityEngine;

namespace Boomtown.WorldGeneration.Editor
{
    public sealed class BoomtownGoldRevealOverlayWindow :
        EditorWindow
    {
        private const string OverlayObjectName =
            "BT_GoldRevealOverlay";

        private const string MaterialPath =
            "Assets/Boomtown/Scripts/WorldGeneration/" +
            "Generated/BT_GoldRevealOverlay.mat";

        private BoomtownGeologyData geologyData;
        private Terrain targetTerrain;

        private GoldRevealMode revealMode =
            GoldRevealMode.Original;

        private float opacity = 0.82f;
        private float intensity = 4f;
        private int revealSpread = 5;
        private float minimumVisibleAlpha = 0.18f;
        public static void Open()
        {
            BoomtownGoldRevealOverlayWindow window =
                GetWindow<
                    BoomtownGoldRevealOverlayWindow>();

            window.titleContent =
                new GUIContent(
                    "Gold Reveal");

            window.minSize =
                new Vector2(
                    430f,
                    360f);

            window.AutoDetect();
        }

        private void OnEnable()
        {
            AutoDetect();
        }

        private void OnFocus()
        {
            if (geologyData == null ||
                targetTerrain == null)
            {
                AutoDetect();
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField(
                "Gold Reveal Overlay",
                new GUIStyle(
                    EditorStyles.boldLabel)
                {
                    fontSize = 17,
                    alignment =
                        TextAnchor.MiddleCenter
                });

            EditorGUILayout.Space(10f);

            EditorGUILayout.HelpBox(
                "Editor/debug view only. It does not reveal gold during " +
                "normal gameplay unless you deliberately leave the overlay " +
                "enabled.",
                MessageType.Info);

            EditorGUILayout.Space(8f);

            geologyData =
                (BoomtownGeologyData)
                EditorGUILayout.ObjectField(
                    "Geology Data",
                    geologyData,
                    typeof(BoomtownGeologyData),
                    false);

            targetTerrain =
                (Terrain)
                EditorGUILayout.ObjectField(
                    "Terrain",
                    targetTerrain,
                    typeof(Terrain),
                    true);

            revealMode =
                (GoldRevealMode)
                EditorGUILayout.EnumPopup(
                    "Display Mode",
                    revealMode);

            opacity =
                EditorGUILayout.Slider(
                    "Opacity",
                    opacity,
                    0.05f,
                    1f);

            intensity =
                EditorGUILayout.Slider(
                    "Intensity",
                    intensity,
                    0.25f,
                    12f);

            revealSpread =
                EditorGUILayout.IntSlider(
                    "Reveal Spread",
                    revealSpread,
                    0,
                    10);

            minimumVisibleAlpha =
                EditorGUILayout.Slider(
                    "Minimum Visible Alpha",
                    minimumVisibleAlpha,
                    0f,
                    0.6f);

            EditorGUILayout.Space(12f);

            if (GUILayout.Button(
                    "Auto Detect Generated Data",
                    GUILayout.Height(32f)))
            {
                AutoDetect();
            }

            EditorGUI.BeginDisabledGroup(
                geologyData == null ||
                targetTerrain == null);

            if (GUILayout.Button(
                    "Create / Refresh Overlay",
                    GUILayout.Height(42f)))
            {
                CreateOrRefresh();
            }

            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button(
                    "Remove Overlay",
                    GUILayout.Height(32f)))
            {
                RemoveOverlay();
            }

            EditorGUILayout.Space(10f);

            if (geologyData != null)
            {
                EditorGUILayout.LabelField(
                    "Original placer gold",
                    $"{geologyData.GetTotalInitialOunces():0.00} oz");

                EditorGUILayout.LabelField(
                    "Remaining placer gold",
                    $"{geologyData.GetTotalRemainingOunces():0.00} oz");

                EditorGUILayout.LabelField(
                    "Recovered placer gold",
                    $"{Mathf.Max(0f, geologyData.GetTotalInitialOunces() - geologyData.GetTotalRemainingOunces()):0.00} oz");
            }
        }

        private void AutoDetect()
        {
            targetTerrain =
                Terrain.activeTerrain;

            if (targetTerrain == null)
            {
                targetTerrain =
                    Object.FindFirstObjectByType<
                        Terrain>();
            }

            string[] districtGuids =
                AssetDatabase.FindAssets(
                    "t:DistrictData",
                    new[]
                    {
                        "Assets/Boomtown/Scripts/" +
                        "WorldGeneration/Generated"
                    });

            foreach (string guid in districtGuids)
            {
                string path =
                    AssetDatabase.GUIDToAssetPath(
                        guid);

                DistrictData district =
                    AssetDatabase.LoadAssetAtPath<
                        DistrictData>(path);

                if (district != null &&
                    district.geologyData != null)
                {
                    geologyData =
                        district.geologyData;

                    break;
                }
            }

            if (geologyData == null)
            {
                string[] geologyGuids =
                    AssetDatabase.FindAssets(
                        "t:BoomtownGeologyData",
                        new[]
                        {
                            "Assets/Boomtown/Scripts/" +
                            "WorldGeneration/Generated"
                        });

                if (geologyGuids.Length > 0)
                {
                    geologyData =
                        AssetDatabase.LoadAssetAtPath<
                            BoomtownGeologyData>(
                            AssetDatabase.GUIDToAssetPath(
                                geologyGuids[0]));
                }
            }

            Repaint();
        }

        private void CreateOrRefresh()
        {
            GameObject overlayObject =
                GameObject.Find(
                    OverlayObjectName);

            if (overlayObject == null)
            {
                overlayObject =
                    new GameObject(
                        OverlayObjectName);

                Undo.RegisterCreatedObjectUndo(
                    overlayObject,
                    "Create Gold Reveal Overlay");
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
                GetOrCreateMaterial();

            overlay.Configure(
                geologyData,
                targetTerrain,
                material,
                revealMode,
                opacity,
                intensity,
                revealSpread,
                minimumVisibleAlpha);

            Selection.activeGameObject =
                overlayObject;

            SceneView.RepaintAll();
        }

        private static void RemoveOverlay()
        {
            GameObject overlayObject =
                GameObject.Find(
                    OverlayObjectName);

            if (overlayObject != null)
            {
                Undo.DestroyObjectImmediate(
                    overlayObject);
            }

            SceneView.RepaintAll();
        }

        private static Material GetOrCreateMaterial()
        {
            Material material =
                AssetDatabase.LoadAssetAtPath<
                    Material>(
                    MaterialPath);

            Shader shader =
                Shader.Find(
                    "Boomtown/Gold Reveal Overlay");

            if (shader == null)
            {
                Debug.LogError(
                    "[Gold Reveal] Shader not found. Confirm " +
                    "BT_GoldRevealOverlay.shader was imported.");

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
                    MaterialPath);
            }
            else if (material.shader != shader)
            {
                material.shader =
                    shader;
            }

            EditorUtility.SetDirty(
                material);

            AssetDatabase.SaveAssets();

            return material;
        }
    }
}
