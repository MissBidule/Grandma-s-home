using PurrNet;
using System;
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


    private GhostClientController m_ghostClientController;
    private GhostMorph m_ghostMorph;
    private GhostInteract m_ghostInteract;
    private QteCircle m_qteCircle;

    private bool isOwner => m_ghostClientController != null && m_ghostClientController.isOwner;

    [SerializeField] private string m_promptMessageValid = "E : Valid";

    /*
     * @brief Awake is called when the script instance is being loaded
     * Gets the PlayerController component.
     * @return void
     */
    void Start()
    {
        m_ghostClientController = GetComponent<GhostClientController>();
        m_ghostMorph = GetComponent<GhostMorph>();
        m_ghostInteract = GetComponentInChildren<GhostInteract>();
    }

    /*
     * @brief OnMove is called by the Input System when movement input is detected
     * @param _context: The context of the input action.
     * @return void
     * [SERVER]
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
     * [LOCAL]
     */

    public void OnLook(InputAction.CallbackContext _context)
    {
        if (!isOwner) return;
        m_lookInputVector = _context.ReadValue<Vector2>();
    }

    /*
     * @brief OnScan is called by the Input System when scan input is detected 
     * @param _context: The context of the input action
     * @return void
     * [LOCAL]
     */
    public void OnScan(InputAction.CallbackContext _context)
    {
        if (!isOwner) return;
        if (_context.performed)
        {
            m_ghostClientController.OnScan();
            InteractPromptUI.m_Instance.Show(m_promptMessageValid);
        }
    }

    /*
     * @brief OnScan is called by the Input System when scan input is detected 
     * @param _context: The context of the input action
     * @return void
     */
    public void OnOpenWheel(InputAction.CallbackContext _context)
    {
        if (!isOwner) return;
        if (_context.performed)
        {
            m_ghostClientController.m_wheel.Toggle();
        }
    }

    /*
     * @brief OnScan is called by the Input System when scan input is detected 
     * @param _context: The context of the input action
     * @return void
     */
    public void OnTransformConfirm(InputAction.CallbackContext _context)
    {
        if (!isOwner) return;
        if (_context.performed)
        {
            
            
            m_ghostClientController.OnMorph();

        }
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
            m_ghostInteract.Interact();
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
            if (!m_qteCircle)
            {
                // Cant place the reference in start cause we need to wait for GhostClientController to spawn the UIHolder
                m_qteCircle = FindAnyObjectByType<QteCircle>();
            }

            if(m_qteCircle.m_isRunning)
            {
                m_qteCircle.CheckSuccess();
            }
        }
    }
}
