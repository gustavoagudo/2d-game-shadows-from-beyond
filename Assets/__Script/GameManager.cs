using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    static GameManager instance;

    // Reset run state whenever Play starts (also when the editor's domain reload is disabled).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        instance = null;
        isGameOn = true;
        levelCurrent = 0;
        healthMax = BaseHearts;
        health = BaseHearts;
        score = 0;
        levelProgressTrigger = false;
        loadingLevel = false;
    }
    // Levels are loaded by name, so the title screen can sit at build index 0.
    public static readonly string[] Levels = { "__Level01", "__Level02", "__Level03" };
    public const string TitleScene = "__Title";
    public static bool isGameOn = true;
    public static int levelCurrent = 0;
    public static int levelMax = 2;
    // Hearts at the start of a run. Each boss defeated adds +1 max heart for the rest of the run.
    public const int BaseHearts = 3;
    public static int healthMax = BaseHearts;
    public static int health = BaseHearts;
    public static int score = 0;
    public static bool levelProgressTrigger = false;
    bool levelLoadedTrigger = true;
    public Image screenShadow;
    public static UnityAction OnScoreChanged;

    void Awake()
    {

        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            // playing a level directly from the editor: keep the level order right
            int i = System.Array.IndexOf(Levels, SceneManager.GetActiveScene().name);
            if (i >= 0) levelCurrent = i;
        }
        else
        {
            // the fade image (ScreenShadow) lives under the persistent GameManager itself, so it is kept as is
            Destroy(gameObject);
        }
    }

    // Start (or restart) the game from the first level with full health and score 0.
    public static void StartNewGame()
    {
        healthMax = BaseHearts;          // a new run starts with the base hearts again
        loadingLevel = false;
        health = healthMax;
        score = 0;
        levelCurrent = 0;
        isGameOn = true;
        levelProgressTrigger = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(Levels[0]);
    }

    // Final boss defeated: victory theme, fade out, back to the title with the end credits rolling.
    public static void FinishGame()
    {
        if (loadingLevel) return;
        levelCurrent = Levels.Length - 1;          // LevelLoadNext after the last level goes to the title
        TitleScreen.RollCreditsOnStart = true;
        GameAudio.LevelComplete(true);
        levelProgressTrigger = true;
    }

    // Back to the title screen; the persistent manager is dropped so a fresh one comes with the next level.
    public static void GoToTitle()
    {
        Time.timeScale = 1f;
        loadingLevel = false;
        isGameOn = true;
        levelProgressTrigger = false;
        if (instance != null)
        {
            Destroy(instance.gameObject);
            instance = null;
        }
        SceneManager.LoadScene(TitleScene);
    }

    void Update()
    {
        // only one level transition at a time (a second request while fading used to skip a level)
        if (levelProgressTrigger)
        {
            levelProgressTrigger = false;
            if (!loadingLevel)
            {
                loadingLevel = true;
                StartCoroutine(WaitAndLevelLoadNext(1f));
            }
        }
        //need to improve
        if (levelLoadedTrigger)
        {
            if (screenShadow != null) screenShadow.rectTransform.parent.gameObject.SetActive(false);
            levelLoadedTrigger = false;
        }
    }

    IEnumerator WaitAndLevelLoadNext(float delay)
    {
        float a;

        if (screenShadow != null)
        {
            screenShadow.rectTransform.parent.gameObject.SetActive(true);
            for (float t = 0f; t < delay; t += Time.deltaTime)
            {
                a = Mathf.Lerp(0f, 1f, t / delay);
                screenShadowSetAlpha(a);
                yield return null;
            }
        }
        else
        {
            yield return new WaitForSeconds(delay);
        }
        LevelLoadNext();
        levelLoadedTrigger = true;
        loadingLevel = false;
    }

    static bool loadingLevel;

    void screenShadowSetAlpha(float alpha)
    {
        Color color = screenShadow.color;

        color.a = alpha;
        screenShadow.color = color;
    }

    public static void ScoreAdd(int scoreToAdd)
    {
        score += scoreToAdd;
        OnScoreChanged?.Invoke();
    }

    public static void LevelLoadNext()
    {
        if (++levelCurrent >= Levels.Length)
        {
            // final boss beaten: back to the title screen
            levelCurrent = 0;
            GoToTitle();
            return;
        }
        SceneManager.LoadScene(Levels[levelCurrent]);
    }

    public void LevelLoad(int level)
    {

    }    
}
