using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class Door : MonoBehaviour
{
    Animator animator;
    public static UnityAction OnGoingNextLevel;

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
    }

    bool used;

    void OnTriggerEnter2D(Collider2D collision)
    {
        // The player has two colliders (capsule + body polygon): each one fires OnTriggerEnter2D,
        // sometimes on different frames. Without this guard the level advanced twice (level 2 -> 3).
        if (used || !GameManager.isGameOn) return;
        if (collision.CompareTag("Player"))
        {
            used = true;
            animator.SetTrigger("Open");
            SoundFXManager.Play("LevelProgress");
            GameAudio.LevelComplete(GameManager.levelCurrent >= GameManager.Levels.Length - 1);
            OnGoingNextLevel!.Invoke();
            GameManager.levelProgressTrigger = true;
        }
    }
}
