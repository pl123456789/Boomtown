using UnityEditor;
using UnityEngine;
using Boomtown.WorldGeneration;
using Boomtown.WorldGeneration.Editor;

public sealed class BoomtownWorldGeneratorWindow : EditorWindow
{
    private BoomtownMapDefinition mapDefinition;

    [MenuItem("Boomtown/World Generator/Open Generator")]
    public static void OpenWindow()
    {
        BoomtownWorldGeneratorWindow window =
            GetWindow<BoomtownWorldGeneratorWindow>();

        window.titleContent =
            new GUIContent("World Generator 2.0");

        window.minSize =
            new Vector2(460f, 420f);
    }

    private void OnEnable()
    {
        AutoDetectMapDefinition();
    }

    private void OnFocus()
    {
        if (mapDefinition == null)
        {
            AutoDetectMapDefinition();
        }
    }

    private void AutoDetectMapDefinition()
    {
        string[] guids =
            AssetDatabase.FindAssets(
                "t:BoomtownMapDefinition",
                new[]
                {
                    "Assets/Boomtown"
                });

        BoomtownMapDefinition fallback = null;

        foreach (string guid in guids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guid);

            BoomtownMapDefinition candidate =
                AssetDatabase.LoadAssetAtPath<
                    BoomtownMapDefinition>(path);

            if (candidate == null)
            {
                continue;
            }

            fallback ??= candidate;

            bool isHope =
                string.Equals(
                    candidate.mapName,
                    "Hope",
                    System.StringComparison
                        .OrdinalIgnoreCase) ||
                string.Equals(
                    candidate.worldSeed,
                    "HOPE1858",
                    System.StringComparison
                        .OrdinalIgnoreCase) ||
                candidate.name.Contains(
                    "Hope1858",
                    System.StringComparison
                        .OrdinalIgnoreCase);

            if (isHope)
            {
                mapDefinition = candidate;
                Repaint();
                return;
            }
        }

        mapDefinition = fallback;
        Repaint();
    }

    private void OnGUI()
    {
        DrawHeader();

        EditorGUILayout.Space(12f);

        mapDefinition =
            (BoomtownMapDefinition)
            EditorGUILayout.ObjectField(
                "Map Definition",
                mapDefinition,
                typeof(BoomtownMapDefinition),
                false);

        EditorGUILayout.Space(10f);

        if (mapDefinition != null)
        {
            EditorGUILayout.LabelField(
                "Current Seed",
                EditorStyles.boldLabel);

            EditorGUILayout.SelectableLabel(
                mapDefinition.worldSeed,
                EditorStyles.textField,
                GUILayout.Height(
                    EditorGUIUtility
                        .singleLineHeight));

            EditorGUILayout.Space(6f);

            if (GUILayout.Button(
                    "Create New Random District Seed",
                    GUILayout.Height(32f)))
            {
                Undo.RecordObject(
                    mapDefinition,
                    "Create Random District Seed");

                mapDefinition.worldSeed =
                    BoomtownWorldGenerator
                        .CreateRandomReplaySeed();

                EditorUtility.SetDirty(
                    mapDefinition);

                AssetDatabase.SaveAssets();

                GUI.FocusControl(null);
            }
        }

        EditorGUILayout.Space(12f);

        EditorGUI.BeginDisabledGroup(
            mapDefinition == null);

        if (GUILayout.Button(
                "Generate Current Seed",
                GUILayout.Height(44f)))
        {
            BoomtownWorldGenerator.GenerateWorld(
                mapDefinition);
        }

        if (GUILayout.Button(
                "Generate New Random District",
                GUILayout.Height(44f)))
        {
            Undo.RecordObject(
                mapDefinition,
                "Generate New Random District");

            mapDefinition.worldSeed =
                BoomtownWorldGenerator
                    .CreateRandomReplaySeed();

            EditorUtility.SetDirty(
                mapDefinition);

            AssetDatabase.SaveAssets();

            BoomtownWorldGenerator.GenerateWorld(
                mapDefinition);
        }

        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space(12f);

        if (mapDefinition == null)
        {
            EditorGUILayout.HelpBox(
                "Select Hope1858 before generating the district.",
                MessageType.Info);

            return;
        }

        EditorGUILayout.HelpBox(
            $"District: {mapDefinition.mapName}\n" +
            $"Region: {mapDefinition.region}\n" +
            $"Year: {mapDefinition.year}\n" +
            $"Seed: {mapDefinition.worldSeed}\n\n" +
            "Generate Current Seed reproduces the same district.\n" +
            "Generate New Random District creates a different replay map.",
            MessageType.None);

        EditorGUILayout.Space(6f);

        EditorGUILayout.HelpBox(
            "Pipeline 2.0 records district identity, terrain, hydrology, " +
            "water, ecology, population, finite geology and validation. " +
            "Watersheds, mineralized mountains and quartz-vein modules have " +
            "reserved fields in DistrictData and can be added without " +
            "replacing the working terrain and river systems.",
            MessageType.Info);
    }

    private static void DrawHeader()
    {
        GUIStyle titleStyle =
            new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment =
                    TextAnchor.MiddleCenter
            };

        GUIStyle subtitleStyle =
            new GUIStyle(EditorStyles.label)
            {
                fontSize = 13,
                alignment =
                    TextAnchor.MiddleCenter
            };

        EditorGUILayout.Space(8f);

        EditorGUILayout.LabelField(
            "BIG STUDIOS",
            titleStyle);

        EditorGUILayout.LabelField(
            "Boomtown District Generator 2.0",
            subtitleStyle);
    }
}
