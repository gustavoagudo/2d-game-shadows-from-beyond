using UnityEditor;
using UnityEditor.SceneManagement;

// In the editor, Play runs the scene that is open, not build index 0.
// This makes Play always start at the title screen (like the built game), whatever scene is being edited.
// Toggle it in the menu GOTY > "Play começa na tela inicial" to test a level directly.
[InitializeOnLoad]
static class PlayFromTitle
{
    const string TitlePath = "Assets/_Scenes/__Title.unity";
    const string PrefKey = "GOTY.PlayFromTitle";
    const string MenuPath = "GOTY/Play começa na tela inicial";

    static PlayFromTitle()
    {
        // delayCall: the asset database is ready by then
        EditorApplication.delayCall += Apply;
    }

    static bool Enabled
    {
        get => EditorPrefs.GetBool(PrefKey, true);
        set => EditorPrefs.SetBool(PrefKey, value);
    }

    static void Apply()
    {
        EditorSceneManager.playModeStartScene = Enabled ? AssetDatabase.LoadAssetAtPath<SceneAsset>(TitlePath) : null;
    }

    [MenuItem(MenuPath)]
    static void Toggle()
    {
        Enabled = !Enabled;
        Apply();
    }

    [MenuItem(MenuPath, true)]
    static bool ToggleValidate()
    {
        Menu.SetChecked(MenuPath, Enabled);
        return true;
    }
}
