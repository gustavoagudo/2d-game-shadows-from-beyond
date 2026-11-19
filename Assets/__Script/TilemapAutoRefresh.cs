using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

// Rebuilds every tilemap's cached tile data and colliders when a scene loads, so the scene always
// reflects the current Tile assets (sprite + collider type) even if the cached data in the scene is stale.
public static class TilemapAutoRefresh
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        RefreshAll();
        SceneManager.sceneLoaded += (scene, mode) => RefreshAll();
    }

    public static void RefreshAll()
    {
        foreach (Tilemap tm in Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            tm.RefreshAllTiles();
            TilemapCollider2D col = tm.GetComponent<TilemapCollider2D>();
            if (col != null) col.ProcessTilemapChanges();
            CompositeCollider2D comp = tm.GetComponent<CompositeCollider2D>();
            if (comp != null) comp.GenerateGeometry();
        }
    }
}
