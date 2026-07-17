using Boomtown.Gameplay.Prospecting;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Boomtown.WorldGeneration.Editor
{
    /// <summary>
    /// Repairs broken GoldPanningController scene references caused by a
    /// deleted/recreated .cs meta GUID. Safe to run repeatedly.
    /// </summary>
    public static class BoomtownProspectingComponentRepair
    {
        [MenuItem(
            "Boomtown/Tools/Repair Prospecting Components")]
        public static void Repair()
        {
            int removedMissingScripts = 0;
            int restoredControllers = 0;
            int assignedGeology = 0;
            int assignedUi = 0;

            BoomtownGeologyData geologyData =
                FindLatestAsset<
                    BoomtownGeologyData>();

            GoldPanningUI panningUi =
                Object.FindFirstObjectByType<
                    GoldPanningUI>(
                    FindObjectsInactive.Include);

            Scene activeScene =
                SceneManager.GetActiveScene();

            foreach (GameObject rootObject in
                     activeScene.GetRootGameObjects())
            {
                Transform[] transforms =
                    rootObject.GetComponentsInChildren<
                        Transform>(true);

                foreach (Transform transform in transforms)
                {
                    GameObject gameObject =
                        transform.gameObject;

                    int missingCount =
                        GameObjectUtility
                            .GetMonoBehavioursWithMissingScriptCount(
                                gameObject);

                    if (missingCount > 0)
                    {
                        Undo.RegisterCompleteObjectUndo(
                            gameObject,
                            "Repair Missing Boomtown Scripts");

                        removedMissingScripts +=
                            GameObjectUtility
                                .RemoveMonoBehavioursWithMissingScript(
                                    gameObject);
                    }

                    GoldInventory inventory =
                        gameObject.GetComponent<
                            GoldInventory>();

                    ProspectingLocationSensor sensor =
                        gameObject.GetComponent<
                            ProspectingLocationSensor>();

                    if (inventory == null ||
                        sensor == null)
                    {
                        continue;
                    }

                    GoldPanningController controller =
                        gameObject.GetComponent<
                            GoldPanningController>();

                    if (controller == null)
                    {
                        controller =
                            Undo.AddComponent<
                                GoldPanningController>(
                                gameObject);

                        restoredControllers++;
                    }

                    SerializedObject serializedController =
                        new SerializedObject(
                            controller);

                    SerializedProperty geologyProperty =
                        serializedController.FindProperty(
                            "geologyData");

                    if (geologyProperty != null &&
                        geologyData != null &&
                        geologyProperty.objectReferenceValue !=
                        geologyData)
                    {
                        geologyProperty.objectReferenceValue =
                            geologyData;

                        assignedGeology++;
                    }

                    SerializedProperty uiProperty =
                        serializedController.FindProperty(
                            "ui");

                    if (uiProperty != null &&
                        panningUi != null &&
                        uiProperty.objectReferenceValue !=
                        panningUi)
                    {
                        uiProperty.objectReferenceValue =
                            panningUi;

                        assignedUi++;
                    }

                    serializedController
                        .ApplyModifiedPropertiesWithoutUndo();

                    EditorUtility.SetDirty(
                        controller);
                }
            }

            EditorSceneManager.MarkSceneDirty(
                activeScene);

            AssetDatabase.SaveAssets();

            Debug.Log(
                $"[Boomtown Repair] Removed {removedMissingScripts} missing " +
                $"script component(s). Restored {restoredControllers} " +
                $"GoldPanningController component(s). Assigned geology to " +
                $"{assignedGeology} and UI to {assignedUi} controller(s).");
        }

        public static void RepairSilentlyAfterGeneration(
            BoomtownGeologyData geologyData)
        {
            GoldPanningUI panningUi =
                Object.FindFirstObjectByType<
                    GoldPanningUI>(
                    FindObjectsInactive.Include);

            GoldInventory[] inventories =
                Object.FindObjectsByType<
                    GoldInventory>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            foreach (GoldInventory inventory in inventories)
            {
                GameObject gameObject =
                    inventory.gameObject;

                ProspectingLocationSensor sensor =
                    gameObject.GetComponent<
                        ProspectingLocationSensor>();

                if (sensor == null)
                {
                    continue;
                }

                if (GameObjectUtility
                        .GetMonoBehavioursWithMissingScriptCount(
                            gameObject) > 0)
                {
                    GameObjectUtility
                        .RemoveMonoBehavioursWithMissingScript(
                            gameObject);
                }

                GoldPanningController controller =
                    gameObject.GetComponent<
                        GoldPanningController>();

                if (controller == null)
                {
                    controller =
                        gameObject.AddComponent<
                            GoldPanningController>();
                }

                SerializedObject serializedController =
                    new SerializedObject(
                        controller);

                SerializedProperty geologyProperty =
                    serializedController.FindProperty(
                        "geologyData");

                if (geologyProperty != null)
                {
                    geologyProperty.objectReferenceValue =
                        geologyData;
                }

                SerializedProperty uiProperty =
                    serializedController.FindProperty(
                        "ui");

                if (uiProperty != null &&
                    panningUi != null)
                {
                    uiProperty.objectReferenceValue =
                        panningUi;
                }

                serializedController
                    .ApplyModifiedPropertiesWithoutUndo();

                EditorUtility.SetDirty(
                    controller);
            }

            EditorSceneManager.MarkSceneDirty(
                SceneManager.GetActiveScene());
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
                    System.IO.File.Exists(path)
                        ? System.IO.File
                            .GetLastWriteTimeUtc(path)
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
                : AssetDatabase.LoadAssetAtPath<T>(
                    newestPath);
        }
    }
}
