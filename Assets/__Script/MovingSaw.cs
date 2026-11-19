using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingSaw : NavController
{
    protected override void Start()
    {
        base.Start();
        LoopEmitter.Attach(gameObject, "SFX_Serra_loop", 0.3f, 6f);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {

        }
    }

}
