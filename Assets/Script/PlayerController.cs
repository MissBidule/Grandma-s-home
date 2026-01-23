using UnityEngine;


/*
@brief       Containts class declaration for PlayerController
@details     The PlayerController class handles player actions by reading input from the PlayerInputController component.
*/
public class PlayerController : MonoBehaviour
{
    private PlayerInputController m_playerInputController;


    private void Awake()
    {
        m_playerInputController = GetComponent<PlayerInputController>();
    }

    /*
    @brief Update triggers every frame
    Gets the movement input vector from the PlayerInputController and moves the player accordingly.
    @return void
    */
    private void Update()
    {
        Vector2 movement = m_playerInputController.m_movementInputVector;
        Vector3 moveDirection = new Vector3(movement.x, 0, movement.y);
        transform.Translate(moveDirection * Time.deltaTime * 5f);
    }
}
