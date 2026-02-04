using UnityEngine;
using UnityEngine.InputSystem;

/*
 * @brief Contains class declaration for PlayerInputController
 * @details The PlayerInputController class handles player input using Unity's Input System.
 */
public class GhostInputController : MonoBehaviour
{
    public Vector2 m_movementInputVector { get; private set; }
    public Vector2 m_lookInputVector { get; private set; }
    private GhostController m_ghostController;
    private GhostMorph m_ghostTransform;

    /*
     * @brief Awake is called when the script instance is being loaded
     * Gets the PlayerController component.
     * @return void
     */
    void Awake()
    {
        m_ghostController = GetComponent<GhostController>();
        m_ghostTransform = GetComponent<GhostMorph>();
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
     * @brief OnScan is called by the Input System when scan input is detected 
     * @param _context: The context of the input action
     * @return void
     */
    public void OnScan(InputAction.CallbackContext _context)
    {
        if (_context.performed)
        {
            m_ghostTransform.ScanForPrefab();
        }
    }

    public void OnOpenWheel(InputAction.CallbackContext _context)
    {
        if (_context.performed)
        {
            WheelController.m_Instance.Toggle();
        }
    }

    public void OnTransformConfirm(InputAction.CallbackContext _context)
    {
        if (_context.performed)
        {
            m_ghostTransform.ConfirmTransform(_context);
        }
    }
}
