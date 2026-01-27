using UnityEngine;
using UnityEngine.InputSystem;

/*
 * @brief       Contains class declaration for PlayerInputController
 * @details     The PlayerInputController class handles player input using Unity's Input System.
 */
public class PlayerInputControllerHit : MonoBehaviour
{

    public Vector2 m_movementInputVector { get; private set; }

    private PlayerControllerHit m_playerControllerHit;


    void Awake()
    {
        m_movementInputVector = Vector2.zero;
        m_playerControllerHit = GetComponent<PlayerControllerHit>();
    }

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
            m_playerControllerHit.Attacks();
        }
        if (_context.canceled)
        {
            m_playerControllerHit.DisableAttack();
        }
    }


    /*
     * @brief OnSwitchWeapon is called by the Input System when switch weapon input is detected
     * @param _context: The context of the input action
     * @return void
     */
    public void OnSwitchWeapon(InputAction.CallbackContext _context)
    {
        if (_context.performed)
        {
            m_playerControllerHit.m_isranged = !m_playerControllerHit.m_isranged;
        }
    }

    /*
     * @brief OnInteract is called by the Input System when interact input is detected
     * @param _context: The context of the input action
     * @return void
     */
    public void OnInteract(InputAction.CallbackContext _context)
    {
        m_playerControllerHit.Clean();
    }

}
