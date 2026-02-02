/**
 * @brief  This class allows movement along the X and Z axes.
 * 
 * The player moves along the axes at a speed of m_speed
 * 
 */
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float m_speed = 5f;

    void Update()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        Vector3 movement = new Vector3(moveX, 0, moveZ);
        transform.Translate(movement * m_speed * Time.deltaTime);
    }
}
