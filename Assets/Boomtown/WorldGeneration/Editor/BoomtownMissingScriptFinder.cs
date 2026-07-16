using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BoomtownMissingScriptFinder
{
    [MenuItem("Boomtown/Tools/Find Missing Scripts")]
    public static void FindMissingScripts()
    {
        int missingCount = 0;

        Scene scene = SceneManager.GetActiveScene();

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            missingCount += ScanGameObject(root);
        }

        if (missingCount == 0)
        {
            Debug.Log("[Boomtown] No missing scripts found in the active scene.");
        }
        else
        {
            Debug.LogWarning(
                $"[Boomtown] Found {missingCount} missing script component(s). " +
                "Click the warnings above to locate each GameObject.");
        }
    }

    [MenuItem("Boomtown/Tools/Remove All Missing Scripts")]
    public static void RemoveAllMissingScripts()
    {
        int removedCount = 0;

        Scene scene = SceneManager.GetActiveScene();

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            removedCount += RemoveMissingScriptsRecursive(root);
        }

        if (removedCount > 0)
        {
            EditorUtility.SetDirty(scene.GetRootGameObjects()[0]);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        }

        Debug.Log(
            $"[Boomtown] Removed {removedCount} missing script component(s) " +
            "from the active scene.");
    }

    private static int ScanGameObject(GameObject gameObject)
    {
        int count = 0;

        Component[] components = gameObject.GetComponents<Component>();

        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] == null)
            {
                count++;

                Debug.LogWarning(
                    $"[Boomtown] Missing script on: {GetHierarchyPath(gameObject)}",
                    gameObject);
            }
        }

        foreach (Transform child in gameObject.transform)
        {
            count += ScanGameObject(child.gameObject);
        }

        return count;
    }

    private static int RemoveMissingScriptsRecursive(GameObject gameObject)
    {
        int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(
            gameObject);

        if (count > 0)
        {
            Undo.RegisterCompleteObjectUndo(
                gameObject,
                "Remove Missing Scripts");

            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(
                gameObject);
        }

        foreach (Transform child in gameObject.transform)
        {
            count += RemoveMissingScriptsRecursive(child.gameObject);
        }

        return count;
    }

    private static string GetHierarchyPath(GameObject gameObject)
    {
        string path = gameObject.name;
        Transform current = gameObject.transform.parent;

        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }
}