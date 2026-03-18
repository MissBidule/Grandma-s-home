using PurrNet;
using PurrNet.Logging;
using Script.UI.Views;
using System;
using UI;
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
    private GhostMorphPreview m_ghostMorphPreview;
    private Interact m_ghostInteract;
    public QteCircle m_qteCircle;

    private bool isOwner => m_ghostClientController != null && m_ghostClientController.isOwner;

    [SerializeField] private string m_promptMessageValid = "F : Valid";

    /*
     * @brief Awake is called when the script instance is being loaded
     * Gets the PlayerController component.
     * @return void
     */
    void Start()
    {
        m_ghostClientController = GetComponent<GhostClientController>();
        m_ghostMorph = GetComponent<GhostMorph>();
        m_ghostMorphPreview = GetComponentInChildren<GhostMorphPreview>();
        m_ghostInteract = GetComponentInChildren<Interact>();
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
            m_ghostClientController.OnOpenWheel();
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

    public void OnRotatePreviewLeft(InputAction.CallbackContext _context) { }
    public void OnRotatePreviewRight(InputAction.CallbackContext _context) { }

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
            m_ghostInteract.OnInteract(m_ghostInteract.m_onFocus);
        }
        else if (_context.canceled)
        {
            m_ghostInteract.StopInteract(m_ghostInteract.m_onFocus);
        }
    }
    
    
    /*
     * @brief OnDash is called by the Input System when dash input is detected
     * @param _context: The context of the input action
     * @return void
     */
    public void OnDash(InputAction.CallbackContext _context)
    {
        if (!isOwner) return;
        if (_context.performed)
        {
            m_ghostClientController.OnDash();
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
            m_ghostClientController.Sneak(true);
        }
        else if (_context.canceled)
        {
            // On release
            m_ghostClientController.Sneak(false);
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
                if (m_qteCircle.CheckSuccess())
                {
                    //QTE finished
                    m_ghostClientController.SabotageNotification();
                }
            }
        }
    }

    public void OnEscape(InputAction.CallbackContext _context)
    {
        if (!isOwner) return;
        if (_context.performed)
        {
            if (!m_qteCircle)
                m_qteCircle = FindAnyObjectByType<QteCircle>();

            if (m_qteCircle != null && m_qteCircle.m_isRunning)
            {
                m_qteCircle.CancelQte();
                return;
            }

            // TODO: ouvrir le menu pause (lucas askip)
        }   
    }
}
