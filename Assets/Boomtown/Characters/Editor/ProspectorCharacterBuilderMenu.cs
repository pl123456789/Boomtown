using UnityEditor;

namespace Boomtown.Characters.Editor
{
    /// <summary>
    /// Provides a unique menu command for Character Generator 3.0 so it cannot
    /// collide with the legacy Prototype 2.0 editor command.
    /// </summary>
    public static class ProspectorCharacterBuilderMenu
    {
        [MenuItem("Boomtown/Characters/Build Character Prototype 3.0", priority = 1)]
        private static void OpenGenerator3()
        {
            ProspectorCharacterBuilder window = EditorWindow.GetWindow<ProspectorCharacterBuilder>();
            window.titleContent = new UnityEngine.GUIContent("Prospector 3.0");
            window.Show();
            window.Focus();
        }
    }
}
