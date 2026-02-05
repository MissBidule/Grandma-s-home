using UnityEngine;
using UnityEngine.UI;

public class WheelSliceHit : MonoBehaviour
{
    void Awake()
    {
        GetComponent<Image>().alphaHitTestMinimumThreshold = 0.5f;
    }
}
