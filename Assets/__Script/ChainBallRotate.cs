using UnityEngine;

public class ChainBallRotate : MonoBehaviour
{
    [SerializeField] bool clockWise;
    float angleCurrent;
    [SerializeField] float delayAtStart = 0.2f;
    float timeElapsed;
    [SerializeField] float swingSpeed = 90f;

    void Start()
    {
        angleCurrent = transform.rotation.z;
    }

    void Update()
    {
        timeElapsed += Time.deltaTime;
        if (timeElapsed < delayAtStart)
        {
            return;
        }

        if (clockWise)
        {
            angleCurrent -= swingSpeed * Time.deltaTime;
        }
        if (!clockWise)
        {
            angleCurrent += swingSpeed * Time.deltaTime;
        }
        transform.rotation = Quaternion.Euler(0, 0, angleCurrent);

        // whoosh once per half turn
        int half = Mathf.FloorToInt(angleCurrent / 180f);
        if (half != lastHalf)
        {
            if (lastHalf != int.MinValue) GameAudio.SfxAt("SFX_BolaEspinhos_Balanco", transform.position, 0.6f, 9f);
            lastHalf = half;
        }
    }

    int lastHalf = int.MinValue;
}
