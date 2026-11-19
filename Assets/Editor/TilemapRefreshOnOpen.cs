using UnityEditor;
using UnityEditor.SceneManagement;

// In the editor, refresh tilemaps when a scene is opened so the Scene view shows the current tile art/colliders.
[InitializeOnLoad]
static class TilemapRefreshOnOpen
{
    static TilemapRefreshOnOpen()
    {
        EditorSceneManager.sceneOpened += (scene, mode) => TilemapAutoRefresh.RefreshAll();
    }
}
