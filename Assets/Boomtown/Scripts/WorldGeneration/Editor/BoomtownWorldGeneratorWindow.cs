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
            new GUIContent("World Generator");

        window.minSize =
            new Vector2(420f, 320f);
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

        EditorGUILayout.Space(12f);

        EditorGUI.BeginDisabledGroup(
            mapDefinition == null);

        if (GUILayout.Button(
                "Generate World",
                GUILayout.Height(46f)))
        {
            BoomtownWorldGenerator.GenerateWorld(
                mapDefinition);
        }

        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space(10f);

        if (mapDefinition == null)
        {
            EditorGUILayout.HelpBox(
                "Select Hope1858 before generating the world.",
                MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox(
                $"Map: {mapDefinition.mapName}\n" +
                $"Region: {mapDefinition.region}\n" +
                $"Year: {mapDefinition.year}\n" +
                $"Seed: {mapDefinition.worldSeed}\n\n" +
                "Modules: Terrain, Paint, River, Forest, " +
                "Spawn, NavMesh, Geology",
                MessageType.None);
        }
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
            "Boomtown World Generator",
            subtitleStyle);
    }
}