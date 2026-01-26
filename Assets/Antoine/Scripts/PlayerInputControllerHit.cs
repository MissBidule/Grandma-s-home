using UnityEngine;
using UnityEngine.InputSystem;

/*
 * @brief       Contains class declaration for PlayerInputController
 * @details     The PlayerInputController class handles player input using Unity's Input System.
 */
public class PlayerInputControllerHit : MonoBehaviour
{

    public Vector2 m_movementInputVector { get; private set; }

    public bool m_isHitting { get; private set; }

    /*
     * @brief OnMove is called by the Input System when movement input is detected
     * @param _context: The context of the input action
     * @return void
     */
    public void OnMove(InputAction.CallbackContext _context)
    {
        m_movementInputVector = _context.ReadValue<Vector2>();
    }

    /*
    @brief function called when the player inputs the hit command
    @param _context: valeur liée à l'input
    @return void
    */
    public void OnHit(InputAction.CallbackContext _context)
    {   
        if (_context.performed)
        {
            m_isHitting = true;
        }
        else if (_context.canceled)
        {
            m_isHitting = false;
        }

    }
}
