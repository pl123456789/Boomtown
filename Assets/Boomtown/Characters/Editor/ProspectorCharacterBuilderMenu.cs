using UnityEditor;
using UnityEngine;

namespace Boomtown.Characters.Editor
{
    /// <summary>
    /// Provides the single supported menu entry for the current character generator.
    /// </summary>
    public static class ProspectorCharacterBuilderMenu
    {
        private const string CurrentMenuPath = "Boomtown/Characters/Character Randomizer";

        [MenuItem(CurrentMenuPath, priority = 1)]
        private static void OpenGenerator()
        {
            ProspectorCharacterBuilder window = EditorWindow.GetWindow<ProspectorCharacterBuilder>();
            window.titleContent = new GUIContent("Character Randomizer");
            window.Show();
            window.Focus();
        }
    }
}
