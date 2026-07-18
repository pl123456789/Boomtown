using UnityEditor;
using UnityEngine;

namespace Boomtown.Characters.Editor
{
    /// <summary>
    /// Provides the single supported menu entry for the current character generator.
    /// </summary>
    public static class ProspectorCharacterBuilderMenu
    {
        private const string CurrentMenuPath = "Boomtown/Characters/Character Generator 3.1";

        [MenuItem(CurrentMenuPath, priority = 1)]
        private static void OpenGenerator()
        {
            ProspectorCharacterBuilder window = EditorWindow.GetWindow<ProspectorCharacterBuilder>();
            window.titleContent = new GUIContent("Character Generator 3.1");
            window.Show();
            window.Focus();
        }
    }
}
