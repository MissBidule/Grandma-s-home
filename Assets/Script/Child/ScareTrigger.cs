using UnityEngine;

/*
 * @brief Prevents being fired with Control collider
 */
public class ScareTrigger : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        GetComponentInParent<ChildController>().CollideWithObject(other);
    }
}
