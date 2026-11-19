using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

// When the editor ends up on an empty "Untitled" scene (e.g. right after the
// project is first opened), open the title screen so Play works right away.
[InitializeOnLoad]
static class OpenFirstLevelOnStartup
{
    const string FirstLevel = "Assets/_Scenes/__Title.unity";

    static OpenFirstLevelOnStartup()
    {
        EditorApplication.delayCall += TryOpen;
    }

    static void TryOpen()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (SceneManager.sceneCount != 1) return;

        Scene active = SceneManager.GetActiveScene();
        bool isEmptyUntitled = string.IsNullOrEmpty(active.path) && active.rootCount <= 2;
        if (!isEmptyUntitled) return;

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(FirstLevel) == null) return;
        EditorSceneManager.OpenScene(FirstLevel, OpenSceneMode.Single);
    }
}
