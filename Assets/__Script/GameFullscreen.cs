using UnityEngine;
using UnityEngine.InputSystem;

// Built game only: always opens in full screen at the display's resolution; the F key toggles full screen / window.
// (A window size saved by an earlier run is ignored. The Unity editor's Game view is not affected.)
public class GameFullscreen : MonoBehaviour
{
    bool fullscreen = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot()
    {
        if (Application.isEditor) return;
        GameObject go = new GameObject("GameFullscreen");
        DontDestroyOnLoad(go);
        go.AddComponent<GameFullscreen>();
    }

    void Start()
    {
        Apply();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            fullscreen = !fullscreen;
            Apply();
        }
    }

    void Apply()
    {
        if (!fullscreen)
        {
            Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
            return;
        }
        int width = Display.main.systemWidth;
        int height = Display.main.systemHeight;
        if (width <= 0 || height <= 0)
        {
            width = Screen.currentResolution.width;
            height = Screen.currentResolution.height;
        }
        // even sizes keep the Pixel Perfect Camera happy
        Screen.SetResolution(width & ~1, height & ~1, FullScreenMode.FullScreenWindow);
    }
}
