using UnityEditor;
using UnityEngine;

namespace Boomtown.Characters.Editor
{
    /// <summary>
    /// Owns the single public menu entry for the current character generator and
    /// removes the legacy prototype command after Unity finishes rebuilding menus.
    /// </summary>
    [InitializeOnLoad]
    public static class ProspectorCharacterBuilderMenu
    {
        private const string LegacyMenuPath = "Boomtown/Characters/Build Character Prototype";
        private const string CurrentMenuPath = "Boomtown/Characters/Character Generator 3.1";

        static ProspectorCharacterBuilderMenu()
        {
            EditorApplication.delayCall += RemoveLegacyMenuItem;
        }

        [MenuItem(CurrentMenuPath, priority = 1)]
        private static void OpenGenerator()
        {
            ProspectorCharacterBuilder window = EditorWindow.GetWindow<ProspectorCharacterBuilder>();
            window.titleContent = new GUIContent("Character Generator 3.1");
            window.Show();
            window.Focus();
        }

        private static void RemoveLegacyMenuItem()
        {
            Menu.RemoveMenuItem(LegacyMenuPath);
        }
    }
}
