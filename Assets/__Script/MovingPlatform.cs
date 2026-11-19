using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingPlatform : NavController
{
    protected override void Start()
    {
        base.Start();
        LoopEmitter.Attach(gameObject, "SFX_Plataforma_Movel_loop", 0.4f, 5f);
    }


    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            collision.transform.parent = this.transform;
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            collision.transform.parent = null;
        }
    }


}
