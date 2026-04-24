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
    private TutoInstructions m_tutoChildInstructions;
    public QteCircle m_qteCircle;
    private int m_lastInteractFrame = -1;

    private bool isOwner => m_ghostClientController != null && m_ghostClientController.isOwner;

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
        m_tutoChildInstructions = GetComponentInChildren<TutoInstructions>();
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
     * @brief OnJump is called by the Input System when jump input is detected
     * @param _context: The context of the input action.
     * @return void
     * [SERVER]
     */
    public void OnJump(InputAction.CallbackContext _context)
    {
        if (!isOwner) return;
        if(m_tutoChildInstructions == null)
        {
            if (_context.performed)
            {
                m_ghostClientController.OnJump();
            }
        }
        else
        {
            if (!m_tutoChildInstructions.m_hasStarted)
            {
                m_ghostClientController.OnJump();
            }
            else
            {
                return;
            }
        }
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
        if (_context.started)
        {
            m_ghostClientController.OnOpenWheel();
        }
        else if (_context.canceled)
        {
            m_ghostClientController.OnCloseWheel();
        }
    }

    public void OnRotatePreviewLeft(InputAction.CallbackContext _context)
    {
        if (!isOwner) return;
        m_ghostMorphPreview.SetRotateLeft(!_context.canceled);
    }

    public void OnRotatePreviewRight(InputAction.CallbackContext _context)
    {
        if (!isOwner) return;
        m_ghostMorphPreview.SetRotateRight(!_context.canceled);
    }

    /*
     * @brief OnInteract is called by the Input System when interact input is detected
     * @param _context: The context of the input action
     * @return void
     */
    public void OnInteract(InputAction.CallbackContext _context)
    {
        if (!isOwner) return;
        // During a QTE, the shared right-click binding must act as Cancel, not Interact.
        if (!m_qteCircle) m_qteCircle = FindAnyObjectByType<QteCircle>();
        if (m_qteCircle != null && m_qteCircle.m_isRunning) return;
        if (_context.performed)
        {
            if (m_ghostClientController.m_wheel != null && m_ghostClientController.m_wheel.IsWheelOpen()) return;

            bool inPreview = m_ghostMorphPreview.m_currentPrefab != null
                             && !GetComponent<GhostMorph>().m_isMorphed;

            if (inPreview)
            {
                // Interact target (revive / sabotage) always wins
                if (m_ghostInteract.m_onFocus != null)
                {
                    m_lastInteractFrame = Time.frameCount;
                    m_ghostInteract.OnInteract(m_ghostInteract.m_onFocus);
                    return;
                }
                // Looking at another scannable = replace preview
                if (m_ghostMorphPreview.IsLookingAtScannable())
                {
                    m_lastInteractFrame = Time.frameCount;
                    m_ghostClientController.OnScan();
                    return;
                }
                // Empty valid spot = morph
                if (m_ghostMorphPreview.m_canMorph)
                {
                    m_lastInteractFrame = Time.frameCount;
                    m_ghostClientController.OnMorph();
                    return;
                }
                // Nothing scannable and cannot morph = drop the preview
                m_lastInteractFrame = Time.frameCount;
                m_ghostMorphPreview.HidePreview();
                InteractPromptUI.m_Instance.Hide();
                return;
            }

            m_lastInteractFrame = Time.frameCount;
            m_ghostInteract.OnInteract(m_ghostInteract.m_onFocus);
        }
        else if (_context.canceled)
        {
            m_ghostInteract.StopInteract(m_ghostInteract.m_onFocus);
        }
    }

    /*
     * @brief OnCancel is called by the Input System when cancel input is detected
     * @param _context: The context of the input action
     * @return void
     */
    public void OnCancel(InputAction.CallbackContext _context)
    {
        if (!isOwner) return;
        if (_context.performed)
        {
            // Prevent double-dispatch when Cancel shares a binding with Interact
            if (m_lastInteractFrame == Time.frameCount) return;
            if (!m_qteCircle) m_qteCircle = FindAnyObjectByType<QteCircle>();
            if (m_qteCircle != null && m_qteCircle.m_isRunning) m_qteCircle.CancelQte();
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
     * @brief OnLeaderboard is called by the Input System when the leaderboard input is held used to display the controls hint
     * @param _context: The context of the input action
     * @return void
     */
    public void OnLeaderboard(InputAction.CallbackContext _context)
    {
        if (!isOwner) return;
        if (!InstanceHandler.TryGetInstance(out UIsManager uisManager))
            return;
        if (_context.performed)
        {
            uisManager.ToggleView<LeaderboardUI>();
        }
        else if (_context.canceled)
        {
            uisManager.ToggleView<LeaderboardUI>();
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

            PauseMenuView.Instance?.OnEscapePressed();
        }   
    }
}
