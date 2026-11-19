using UnityEngine;

public class GameOver : MonoBehaviour
{
    // "RETRY": start over from the first level with full health
    public void GameRestart()
    {
        GameAudio.Sfx("SFX_UI_Confirmar", 0.8f, 0f);
        gameObject.SetActive(false);
        GameManager.StartNewGame();
        BGMManager.PlayBGM(true);
    }

    // "MENU": back to the title screen
    public void GameQuit()
    {
        GameAudio.Sfx("SFX_UI_Voltar", 0.8f, 0f);
        gameObject.SetActive(false);
        GameManager.GoToTitle();
    }
}
