using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChainBall : MonoBehaviour
{
    [SerializeField] float angleStart = -90;
    [SerializeField] float angleEnd = 90;
    float angleCurrent;
    [SerializeField] float delayAtStart = 0.2f;
    float timeElapsed;
    bool swingRight;
    [SerializeField] float swingSpeed = 90f;

    void Start()
    {
        if (angleStart > angleEnd)
        {
            swingRight = true;
        }
        angleCurrent = transform.rotation.z;
    }

    void Update()
    {
        timeElapsed += Time.deltaTime;
        if (timeElapsed < delayAtStart)
        {
            return;
        }

        if (swingRight)
        {
            angleCurrent += swingSpeed * Time.deltaTime;
            if (angleCurrent >= angleEnd)
            {
                swingRight = false;
            }
        }
        if (!swingRight)
        {
            angleCurrent -= swingSpeed * Time.deltaTime;
            if (angleCurrent <= angleStart)
            {
                swingRight = true;
            }
            
        }
        transform.rotation = Quaternion.Euler(0, 0, angleCurrent);

        // whoosh each time the ball swings through the bottom of the arc
        if (Mathf.Sign(angleCurrent) != Mathf.Sign(anglePrev) && anglePrev != 0f)
        {
            GameAudio.SfxAt("SFX_BolaEspinhos_Balanco", transform.position, 0.7f, 9f);
        }
        anglePrev = angleCurrent;
    }

    float anglePrev;
}
