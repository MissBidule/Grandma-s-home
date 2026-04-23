using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

/*
 * @brief  Guided tutorial manager.
 * @details Each step shows an instruction and waits for the player to complete the
 *          required action before advancing.
 *          Ghost phase, then a fade switches POV to the child.
 */
public class TutoManager : MonoBehaviour
{
    GhostMorph m_ghostMorphTuto;
    [Header("Players")]
    [SerializeField] private GhostController m_ghost;
    [SerializeField] private GhostController m_ghostTuto;
    [SerializeField] private ChildController m_child;

    [Header("Tutorial Objects")]
    [SerializeField] private SabotageObject m_sabotageObject;
    [SerializeField] private GameObject[] m_scanObjects;
    [SerializeField] private TransformOption[] m_wheelFillOptions;
    [SerializeField] private GameObject m_untransformPropsTuto;
    private GameObject m_instance;

    [Header("UI")]
    [SerializeField] private TutoUIController m_ui;

    private class TutoStep
    {
        public string message;
        public Func<bool> condition;
        public Action onEnter;
    }

    private List<TutoStep> m_steps = new();
    private int m_currentStep = -1;
    private bool m_waitingForFade = false;

    private GhostMorph m_ghostMorph;
    private GhostClientController m_ghostClient;
    private GhostSimulateMovement m_ghostSim;
    private PlayerInput m_ghostInput;
    private ChildClientController m_childClient;
    private PlayerInput m_childInput;
    private int m_childPhaseStart;

    // Called by TutoPlayerSpawningState after spawn
    public void Init(GhostController _ghost, ChildController _child, GhostController _ghostTuto)
    {
        m_ghost = _ghost;
        m_child = _child;
        m_ghostTuto = _ghostTuto;
        m_ghostTuto.m_isStopped = false;

        m_ghostMorph = m_ghost.GetComponent<GhostMorph>();
        m_ghostMorphTuto = m_ghostTuto.GetComponent<GhostMorph>();
        m_ghostClient = m_ghost.GetComponent<GhostClientController>();
        m_ghostSim = m_ghost.GetComponent<GhostSimulateMovement>();
        m_ghostInput = m_ghost.GetComponent<PlayerInput>();
        m_childClient = m_child.GetComponent<ChildClientController>();
        m_childInput = m_child.GetComponent<PlayerInput>();

        m_sabotageObject ??= FindAnyObjectByType<SabotageObject>();

        m_sabotageObject?.SetSabotable(false);
        SetScanObjectsEnabled(false);
        SetScanOutline(false);
        SetBreakable(false);

        BuildSteps();
        StartCoroutine(StartAfterFrame());
    }

    private IEnumerator StartAfterFrame()
    {
        yield return null;

        yield return new WaitUntil(() =>
            (m_ghostClient == null || m_ghostClient.m_uiHolder != null) &&
            (m_childClient == null || m_childClient.m_uiHolder != null));

        SetPlayerActive(m_ghost.gameObject, true);
        SetPlayerActive(m_child.gameObject, false);
        if (m_ghostClient?.m_uiHolder != null) m_ghostClient.m_uiHolder.SetActive(true);
        if (m_childClient?.m_uiHolder != null) m_childClient.m_uiHolder.SetActive(false);

        var ghostTutoClient = m_ghostTuto?.GetComponent<GhostClientController>();
        if (ghostTutoClient?.m_playerCamera != null)
        {
            var vol = ghostTutoClient.m_playerCamera.GetComponent<UnityEngine.Rendering.Volume>();
            if (vol != null) vol.enabled = false;
        }

        EnterStep(0);
    }

    private void Update()
    {
        if (m_waitingForFade) return;
        if (m_currentStep < 0 || m_currentStep >= m_steps.Count) return;

        if (m_steps[m_currentStep].condition())
            Advance();
    }

    private void Advance()
    {
        int next = m_currentStep + 1;

        if (next >= m_steps.Count)
        {
            m_ui.ShowText("Tutorial <b><color=#5AB4FF>complete</color></b>! If you are done press <b><color=#5AB4FF>Esc</color></b> to <b><color=#5AB4FF>exit</color></b>.");
            m_currentStep = m_steps.Count;
            return;
        }

        if (next == m_childPhaseStart)
        {
            m_waitingForFade = true;
            m_ui.HideText();
            m_ui.FadeAndSwitch(
                _onBlack: SwitchToChild,
                _onDone: () => { m_waitingForFade = false; EnterStep(next); }
            );
            return;
        }

        EnterStep(next);
    }

    private void EnterStep(int _index)
    {
        m_currentStep = _index;
        TutoStep step = m_steps[_index];
        step.onEnter?.Invoke();
        m_ui.ShowText(ProcessBindings(step.message));
    }

    private void UntransformGhostTuto()
    {
        m_ghostTuto.gameObject.SetActive(true);
        SetRenderingOutline(m_ghostTuto.gameObject, "Outline_1", true);

        m_ghostMorphTuto.m_isMorphed = true;
        Transform corpsGhostTuto = m_ghostTuto.gameObject.transform.Find("ghost_tpose/corps_F");

        BoxCollider boxCollider = m_ghostTuto.gameObject.GetComponent<BoxCollider>();
        boxCollider.enabled = false;
        if (corpsGhostTuto != null)
            corpsGhostTuto.gameObject.SetActive(false);

        if (m_untransformPropsTuto != null)
        {
            m_instance = Instantiate(m_untransformPropsTuto, m_ghostTuto.transform);
            m_instance.transform.localPosition = Vector3.zero;
        }
        m_ghostTuto.m_isSlowed = false;
    }


    private void BuildSteps()
    {
        // GHOST PHASE
        bool movedForward = false, movedBack = false, movedLeft = false, movedRight = false;
        float moveTimer = 0f;
        m_steps.Add(new TutoStep
        {
            message = "Move around the room using <b><color=#5AB4FF>Z Q S D</color></b>",
            condition = () =>
            {
                Vector2 move = m_ghostInput.actions["Move"].ReadValue<Vector2>();
                if (move.y > 0.1f)  movedForward = true;
                if (move.y < -0.1f) movedBack    = true;
                if (move.x < -0.1f) movedLeft    = true;
                if (move.x > 0.1f)  movedRight   = true;
                if (movedForward && movedBack && movedLeft && movedRight && move.sqrMagnitude > 0.01f)
                    moveTimer += Time.deltaTime;
                return moveTimer >= 1.2f;
            }
        });

        float climbTimer = 0f;
        m_steps.Add(new TutoStep
        {
            message = "Ghosts can <b><color=#5AB4FF>climb walls</color></b>. Try climbing a wall by walking into it.",
            condition = () =>
            {
                if (m_ghostSim != null && m_ghostSim.m_isClimbing)
                    climbTimer += Time.deltaTime;
                else
                    climbTimer = 0f;
                return climbTimer >= 0.4f;
            }
        });

        bool hasDashed = false;
        m_steps.Add(new TutoStep
        {
            message = "<b><color=#5AB4FF>Dash</color></b> with [{Ghost.Dash}] to move quickly.\nDash recharges after <b><color=#5AB4FF>20s</color></b> or instantly on a successful <b><color=#5AB4FF>sabotage</color></b>.",
            condition = () =>
            {
                if (m_ghost.m_isDashing) hasDashed = true;
                return hasDashed;
            }
        });

        m_steps.Add(new TutoStep
        {
            message = "<b><color=#5AB4FF>Sabotage</color></b> the highlighted object with [{Ghost.Interact}], use [{Ghost.Validate}] to validate.\nThis will increase the <b><color=#5AB4FF>sabotage bar</color></b> over time.",
            onEnter = () => m_sabotageObject?.SetSabotable(true),
            condition = () => m_sabotageObject != null && m_sabotageObject.m_isSabotaged
        });

        m_steps.Add(new TutoStep
        {
            message = "<b><color=#5AB4FF>Scan 3</color></b> highlighted objects with [{Ghost.Scan}]",
            onEnter = () => { SetScanObjectsEnabled(true); SetScanOutline(true); },
            condition = () =>
            {
                var wheel = m_ghostClient?.m_wheel;
                if (wheel == null) return false;
                return wheel.m_wheelButtons.Count(b => !b.IsEmpty()) >= 3;
            }
        });
        
        m_steps.Add(new TutoStep
        {
            message = "<b><color=#5AB4FF>Confirm</color></b> the transformation with [{Ghost.TransformConfirm}] to <b><color=#5AB4FF>hide yourself</color></b>.",
            onEnter = () => { SetScanObjectsEnabled(false); SetScanOutline(false); },
            condition = () => m_ghostMorph != null && m_ghostMorph.m_isMorphed
        });

        m_steps.Add(new TutoStep
        {
            message = "Move to <b><color=#5AB4FF>untransform</color></b>.",
            condition = () => m_ghostMorph != null && !m_ghostMorph.m_isMorphed
        });

        m_steps.Add(new TutoStep
        {
            message = "Open the <b><color=#5AB4FF>wheel</color></b> by pressing and holding [{Ghost.OpenProps}], select a <b><color=#5AB4FF>transformation</color></b> by hovering over it and releasing the key.",
            onEnter = () =>
            {
                var wheel = m_ghostClient?.m_wheel;
                if (wheel != null) wheel.m_selectedPrefab = null;
            },
            condition = () =>
            {
                var wheel = m_ghostClient?.m_wheel;
                return wheel != null && wheel.m_selectedPrefab != null;
            }
        });

        bool pressedLeft = false, pressedRight = false;
        m_steps.Add(new TutoStep
        {
            message = "<b><color=#5AB4FF>Rotate</color></b> the preview using [{Ghost.RotatePreviewLeft}] and [{Ghost.RotatePreviewRight}].",
            onEnter = () =>
            {
                if (m_ghostClient != null) m_ghostClient.m_morphBlocked = true;
            },
            condition = () =>
            {
                if (m_ghostInput == null) return false;
                if (m_ghostInput.actions["RotatePreviewLeft"].IsPressed())  pressedLeft  = true;
                if (m_ghostInput.actions["RotatePreviewRight"].IsPressed()) pressedRight = true;
                return pressedLeft && pressedRight;
            }
        });

        m_steps.Add(new TutoStep
        {
            message = "<b><color=#5AB4FF>Confirm</color></b> the transformation with [{Ghost.TransformConfirm}] to <b><color=#5AB4FF>hide yourself</color></b>.",
            onEnter = () =>
            {
                if (m_ghostClient != null) m_ghostClient.m_morphBlocked = false;
            },
            condition = () => m_ghostMorph != null && m_ghostMorph.m_isMorphed
        });

        m_steps.Add(new TutoStep
        {
            message = "Move to <b><color=#5AB4FF>untransform</color></b>.",
            condition = () => m_ghostMorph != null && !m_ghostMorph.m_isMorphed
        });

        m_steps.Add(new TutoStep
        {
            message = "Your <b><color=#5AB4FF>wheel is full</color></b>! <b><color=#5AB4FF>Scan</color></b> another object with [{Ghost.Scan}], then select a slot to <b><color=#5AB4FF>replace</color></b> it by pressing [{Ghost.OpenProps}].",
            onEnter = () =>
            {
                var wheel = m_ghostClient?.m_wheel;
                if (wheel != null)
                {
                    wheel.m_selectedPrefab = null;
                    if (m_wheelFillOptions != null)
                    {
                        for (int i = 0; i < wheel.m_wheelButtons.Count && i < m_wheelFillOptions.Length; i++)
                            wheel.m_wheelButtons[i].UpdateTransformOption(m_wheelFillOptions[i]);
                    }
                }
                SetScanObjectsEnabled(true);
                SetScanOutline(true);
            },
            condition = () =>
            {
                var wheel = m_ghostClient?.m_wheel;
                return wheel != null && wheel.m_selectedPrefab != null;
            }
        });

        m_steps.Add(new TutoStep
        {
            message = "<b><color=#5AB4FF>Confirm</color></b> the transformation with [{Ghost.TransformConfirm}].",
            onEnter = () => { SetScanObjectsEnabled(false); SetScanOutline(false); },
            condition = () => m_ghostMorph != null && m_ghostMorph.m_isMorphed
        });

        m_steps.Add(new TutoStep
        {
            message = "<b><color=#5AB4FF>Revive</color></b> the ghost on the ground by pressing and holding [{Ghost.Interact}].",
            onEnter = () =>
            {
                m_ghost.GetComponentInChildren<GhostMorphPreview>()?.HidePreview();
                m_ghostClient?.m_wheel?.ClearSelection();
                m_ghostTuto.m_isStopped = true;
                SetRenderingOutline(m_ghostTuto.gameObject, "Outline_1", true);
            },
            condition = () => m_ghostTuto != null && !m_ghostTuto.m_isStopped
        });

        // CHILD PHASE
        m_childPhaseStart = m_steps.Count;

        m_steps.Add(new TutoStep
        {
            message = "<b><color=#5AB4FF>Repair</color></b> the sabotage with [{Child.Interact}], use [{Child.Validate}] to validate.",
            condition = () => m_sabotageObject != null && !m_sabotageObject.m_isSabotaged
        });

        float sneakTimer = 0f;
        m_steps.Add(new TutoStep
        {
            message = "<b><color=#5AB4FF>Sneak</color></b> with [{Child.Sneak}] to move <b><color=#5AB4FF>silently</color></b>.",
            condition = () =>
            {
                if (m_childInput != null && m_childInput.actions["Sneak"].IsPressed())
                    sneakTimer += Time.deltaTime;
                else
                    sneakTimer = 0f;
                return sneakTimer >= 2f;
            }
        });

        m_steps.Add(new TutoStep
        {
            message = "Hit <b><color=#5AB4FF>3 objects</color></b> with [{Child.Attack}].\n<b><color=#5AB4FF>Warning</color></b>: it costs <b><color=#5AB4FF>money</color></b>!",
            onEnter = () =>
            {
                SetScanOutline(true);
                SetBreakable(true);
            },
            condition = () => m_scanObjects != null &&
                m_scanObjects.Count(obj =>
                {
                    if (obj == null) return false;
                    BrokeDecor bd = obj.GetComponentInChildren<BrokeDecor>();
                    return bd != null && bd.m_isBroken;
                }) >= 3
        });

        bool initialRanged = false;
        m_steps.Add(new TutoStep
        {
            message = "<b><color=#5AB4FF>Change weapon</color></b> with [{Child.Change_weapon}].",
            onEnter = () =>
            {
                SetScanOutline(false);
                initialRanged = m_child.m_isRanged;
                if (m_childClient != null) m_childClient.m_weaponSwapBlocked = false;
            },
            condition = () => m_child.m_isRanged != initialRanged
        });

        m_steps.Add(new TutoStep
        {
            message = "A ghost is <b><color=#5AB4FF>hidden</color></b> in a mop, shoot him to <b><color=#5AB4FF>untransform</color></b> him.",
            onEnter = UntransformGhostTuto,
            condition = () => !m_ghostMorphTuto.m_isMorphed
        });

        bool initialRanged2 = false;
        m_steps.Add(new TutoStep
        {
            message = "Shoot the ghost to <b><color=#5AB4FF>slow</color></b> it down with [{Child.Attack}].",
            onEnter = () =>
            {
                m_instance.SetActive(false);
                initialRanged2 = m_child.m_isRanged;
                if (m_childClient != null) m_childClient.m_weaponSwapBlocked = true;
            },
            condition = () => m_ghostTuto.m_isSlowed
        });

        m_steps.Add(new TutoStep
        {
            message = "Switch back to <b><color=#5AB4FF>melee</color></b> weapon with [{Child.Change_weapon}].",
            onEnter = () =>
            {
                if (m_childClient != null) m_childClient.m_weaponSwapBlocked = false;
            },
            condition = () => m_child.m_isRanged != initialRanged2
        });

        m_steps.Add(new TutoStep
        {
            message = "<b><color=#5AB4FF>Knock out</color></b> the ghost with [{Child.Attack}].\n<b><color=#5AB4FF>Warning</color></b>: if the ghost touches you, you'll be <b><color=#5AB4FF>scared</color></b> slowed and unable to attack!",
            condition = () => m_ghostTuto != null && m_ghostTuto.m_isStopped
        });
    }

    // POV switch

    private void SwitchToChild()
    {
        m_ghost.gameObject.SetActive(false);
        m_ghostTuto.gameObject.SetActive(false);
        m_child.gameObject.SetActive(true);
        SetPlayerActive(m_child.gameObject, true);

        if (m_childClient != null) m_childClient.m_weaponSwapBlocked = true;

        SetUIHolderActive("GhostUIHolder(Clone)", false);
        SetUIHolderActive("ChildUIHolder(Clone)", true);
    }

    private void SetUIHolderActive(string _name, bool _active)
    {
        foreach (GameObject obj in FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (obj.name == _name)
                obj.SetActive(_active);
        }
    }

    private void SetPlayerActive(GameObject _player, bool _active)
    {
        PlayerInput input = _player.GetComponent<PlayerInput>();
        if (input != null) input.enabled = _active;

        CinemachineCamera cam = _player.GetComponentInChildren<CinemachineCamera>();
        if (cam != null) cam.enabled = _active;

        AudioListener audio = _player.GetComponentInChildren<AudioListener>();
        if (audio != null) audio.enabled = _active;
    }

    // scan object helpers

    private void ForEachScanObject(Action<GameObject> _action)
    {
        if (m_scanObjects == null) return;
        foreach (GameObject obj in m_scanObjects)
            if (obj != null) _action(obj);
    }

    private void SetScanObjectsEnabled(bool _active) =>
        ForEachScanObject(obj => { var s = obj.GetComponentInChildren<ScannableObject>(); if (s) s.m_isScannable = _active; });

    private void SetScanOutline(bool _active) =>
        ForEachScanObject(obj => { foreach (Outline o in obj.GetComponentsInChildren<Outline>()) o.enabled = _active; });

    private void SetBreakable(bool _active) =>
        ForEachScanObject(obj => { var bd = obj.GetComponentInChildren<BrokeDecor>(); if (bd) bd.m_isBreakable = _active; });


    private void SetRenderingOutline(GameObject _target, string _layerName, bool _active)
    {
        uint layer = RenderingLayerMask.GetMask(_layerName);
        foreach (Renderer r in _target.GetComponentsInChildren<Renderer>())
        {
            if (_active) r.renderingLayerMask |= layer;
            else r.renderingLayerMask &= ~layer;
        }
    }

    private string ProcessBindings(string _message)
    {
        return System.Text.RegularExpressions.Regex.Replace(_message, @"\{(.*?)\}", match =>
        {
            string[] parts = match.Groups[1].Value.Split('.');
            if (parts.Length != 2) return match.Value;
            return $"<b><color=#5AB4FF>{InputBindingHelper.GetKey(parts[0], parts[1])}</color></b>";
        });
    }
}
