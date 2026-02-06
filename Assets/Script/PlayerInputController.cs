using UnityEngine;
using UnityEngine.InputSystem;

/*
 * @brief Contains class declaration for PlayerInputController
 * @details The PlayerInputController class handles player input using Unity's Input System.
 */
public class PlayerInputController : MonoBehaviour
{
    public Vector2 m_movementInputVector { get; private set; }
    public Vector2 m_lookInputVector { get; private set; }
    private PlayerController m_playerController;

    /*
     * @brief Awake is called when the script instance is being loaded
     * Gets the PlayerController component.
     * @return void
     */
    void Awake()
    {
        m_playerController = GetComponent<PlayerController>();
    }

    /*
     * @brief OnMove is called by the Input System when movement input is detected
     * @param _context: The context of the input action.
     * @return void
     */
    public void OnMove(InputAction.CallbackContext _context)
    {
        m_movementInputVector = _context.ReadValue<Vector2>();
    }

    /*
     * @brief OnLook is called by the Input System when camera movement input is detected
     * @param _context: The context of the input action
     * @return void
     */

    public void OnLook(InputAction.CallbackContext _context)
    {
        m_lookInputVector = _context.ReadValue<Vector2>();
    }

    /*
     * @brief Resets the movement input to zero
     * Used when anchoring the player to prevent immediate movement detection.
     * @return void
     */
    public void ResetMovementInput()
    {
        m_movementInputVector = Vector2.zero;
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
            m_playerController.Attacks();
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
            m_playerController.m_isranged = !m_playerController.m_isranged;
        }
    }

    /*
     * @brief OnInteract is called by the Input System when interact input is detected
     * @param _context: The context of the input action
     * @return void
     */
    public void OnInteract(InputAction.CallbackContext _context)
    {
        m_playerController.Clean();
    }

    /*
     * @brief OnScan is called by the Input System when scan input is detected 
     * @param _context: The context of the input action
     * @return void
     */
    
}
