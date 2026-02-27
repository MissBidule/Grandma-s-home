using PurrNet;
using UnityEngine;
using UnityEngine.InputSystem;

/*
 * @brief Contains class declaration for ChildInputController
 * @details The ChildInputController class handles child input using Unity's Input System.
 */
public class ChildInputController : MonoBehaviour
{
    public Vector2 m_movementInputVector { get; private set; }
    public Vector2 m_lookInputVector { get; private set; }

    public ChildClientController m_childClientController;

    private bool isOwner => m_childClientController != null && m_childClientController.isOwner;
    /*
     * @brief Awake is called when the script instance is being loaded
     * Gets the ChildController component.
     * @return void
     */
    void Awake()
    {
        m_childClientController = GetComponent<ChildClientController>();
    }

    /*
     * @brief OnMove is called by the Input System when movement input is detected
     * @param _context: The context of the input action.
     * @return void
     */
    public void OnMove(InputAction.CallbackContext _context)
    {
        if (!isOwner) return;
        m_movementInputVector = _context.ReadValue<Vector2>();
    }

    /*
     * @brief OnLook is called by the Input System when camera movement input is detected
     * @param _context: The context of the input action
     * @return void
     */

    public void OnLook(InputAction.CallbackContext _context)
    {
        if (!isOwner) return;
        m_lookInputVector = _context.ReadValue<Vector2>();
    }


    /*
     * @brief function called when the child inputs the jump command
     * @param _context: value linked to input
     * @return void
     */
    public void OnJump(InputAction.CallbackContext _context)
    {
        if (!isOwner) return;
        if (_context.performed)
        {
            m_childClientController.OnJump();
        }
    }


    /*
    @brief function called when the child inputs the hit command
    @param _context: value linked to input
    @return void
    */
    public void OnAttack(InputAction.CallbackContext _context)
    {
        if (!isOwner) return;
        if (_context.performed)
        {
            m_childClientController.OnAttack();
        }
    }

    /*
     * @brief OnSwitchWeapon is called by the Input System when switch weapon input is detected
     * @param _context: The context of the input action
     * @return void
     */
    public void OnSwitchWeapon(InputAction.CallbackContext _context)
    {
        if (!isOwner) return;
        if (_context.performed)
        {
            m_childClientController.OnSwitchWeapon();
        }
    }
}

