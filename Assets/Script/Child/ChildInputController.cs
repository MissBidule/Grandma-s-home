using PurrNet;
using Script.UI.Views;
using UI;
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
    private Interact m_childInteract;

    public ChildClientController m_childClientController;
    private QteCircle m_qteCircle;


    private bool isOwner => m_childClientController != null && m_childClientController.isOwner;
    
    /*
     * @brief Awake is called when the script instance is being loaded
     * Gets the ChildController component.
     * @return void
     */
    void Awake()
    {
        m_childClientController = GetComponent<ChildClientController>();
        m_childInteract = GetComponentInChildren<Interact>();
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
     * @brief OnInteract is called by the Input System when interact input is detected
     * @param _context: The context of the input action
     * @return void
     */
    public void OnInteract(InputAction.CallbackContext _context)
    {
        if (!isOwner) return;
        if (_context.performed)
        {
            m_childInteract.OnInteract(m_childInteract.m_onFocus);
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
    
    /*
     * @brief OnHint is called by the Input System when hint input is detected used to display the controls hint
     * @param _context: The context of the input action
     * @return void
     */
    public void OnHint(InputAction.CallbackContext _context)
    {
        if (!isOwner) return;
        if (_context.performed)
        {
            if (!InstanceHandler.TryGetInstance(out UIsManager uisManager))
                return;
            
            uisManager.ToggleView<InstructionsView>();
        }
    }
    
    /*
     * @brief OnSneak  is called by the Input System when sneak input is detected
     * @param _context: The context of the input action
     * @return void
     */
    public void OnSneak(InputAction.CallbackContext _context)
    {
        if (!isOwner) return;
        if (_context.started)
        {
            // On press
            m_childClientController.Sneak(true);
        }
        else if (_context.canceled)
        {
            // On release
            m_childClientController.Sneak(false);
        }
    }

    public void OnPushToTalk(InputAction.CallbackContext _context)
    {
        AudioManager audioManager = FindFirstObjectByType<AudioManager>();
        if (!isOwner) return;
        if (_context.started)
        {
            // On press
            audioManager.PushToTalk(true);
            Debug.Log("il commence a presser");
        }
        else if (_context.canceled)
        {
            // On release
            audioManager.PushToTalk(false);
            Debug.Log("il a finie de presser");
        }
    }

    public void OnJump(InputAction.CallbackContext _context)
    {
        if (!isOwner) return;
        if (_context.performed)
        {
            m_childClientController.OnJump();
        }
    }

    /*
     * @brief OnValidate is called by the Input System when validate input is detected
     * @param _context: The context of the input action
     * @return void
     */
    public void OnValidation(InputAction.CallbackContext _context)
    {
        if (!isOwner) return;
        if (_context.performed)
        {
            m_childClientController.OnValidation();
        }
    }
}

