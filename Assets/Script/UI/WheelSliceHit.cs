using PurrNet;
using UnityEngine;
using UnityEngine.UI;

public class WheelSliceHit : NetworkBehaviour
{
    void Awake()
    {
        GetComponent<Image>().alphaHitTestMinimumThreshold = 0.5f;
    }
}
