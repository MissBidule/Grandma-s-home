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
    private GhostMorphPreview m_ghostMorphPreview;
    private Dictionary<ScannableObject, Sprite> m_savedScanIcons = new();
    private int m_childPhaseStart;

    // Called by TutoPlayerSpawningState after spawn
    public void Init(GhostController _ghost, ChildController _child, GhostController _ghostTuto)
    {
        m_ghost = _ghost;
        m_child = _child;
        m_ghostTuto = _ghostTuto;
        m_ghostTuto.m_isStopped = false;

        m_ghostMorph = m_ghost.GetComponent<GhostMorph>();
        m_ghostMorphPreview = m_ghost.GetComponentInChildren<GhostMorphPreview>();
        m_ghostMorphTuto = m_ghostTuto.GetComponent<GhostMorph>();

        m_sabotageObject ??= FindAnyObjectByType<SabotageObject>();

        m_sabotageObject?.SetSabotable(false);
        SetScanObjectsEnabled(false);
        SetBrokeDecorEnabled(false);
        SetScanOutline(false);

        BuildSteps();
        StartCoroutine(StartAfterFrame());
    }

    private IEnumerator StartAfterFrame()
    {
        yield return null;

        var ghostClient = m_ghost.GetComponent<GhostClientController>();
        var childClient = m_child.GetComponent<ChildClientController>();

        yield return new WaitUntil(() =>
            (ghostClient == null || ghostClient.m_uiHolder != null) &&
            (childClient == null || childClient.m_uiHolder != null));

        SetPlayerActive(m_ghost.gameObject, true);
        SetPlayerActive(m_child.gameObject, false);
        if (ghostClient?.m_uiHolder != null) ghostClient.m_uiHolder.SetActive(true);
        if (childClient?.m_uiHolder != null) childClient.m_uiHolder.SetActive(false);

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
            m_ui.ShowText("Tutorial complete! If you are done press Esc to exit.");
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
        m_ghostTuto.gameObject.SetActive(true); SetRenderingOutline(m_ghostTuto.gameObject, "Outline_1", true);

        m_ghostMorphTuto.m_isMorphed= true;
        Transform m_corpsGhostTuto = m_ghostTuto.gameObject.transform.Find("ghost_tpose/corps_F");

        BoxCollider boxCollider =m_ghostTuto.gameObject.GetComponent<BoxCollider>();
        boxCollider.enabled=false;
        if(m_corpsGhostTuto != null)
        {
            m_corpsGhostTuto.gameObject.SetActive(false);
        }
        if(m_untransformPropsTuto != null)
        {
            m_instance = Instantiate(m_untransformPropsTuto, m_ghostTuto.transform);
            m_instance.transform.localPosition = Vector3.zero;
        }
        m_ghostTuto.m_isSlowed = false;
    }

    private bool VerifieUntransform()
    {
        GhostMorph ghostMorphTuto = m_ghostTuto.GetComponent<GhostMorph>();
        return !ghostMorphTuto.m_isMorphed;
    }
    private void BuildSteps()
    {
        // GHOST PHASE
        bool movedForward = false, movedBack = false, movedLeft = false, movedRight = false;
        m_steps.Add(new TutoStep
        {
            message = "Move around the room using Z Q S D",
            condition = () =>
            {
                Vector3 d = m_ghost.m_wishDir;
                if (d.z > 0.1f)  movedForward = true;
                if (d.z < -0.1f) movedBack    = true;
                if (d.x < -0.1f) movedLeft    = true;
                if (d.x > 0.1f)  movedRight   = true;
                return movedForward && movedBack && movedLeft && movedRight;
            }
        });

        bool hasClimbed = false;
        m_steps.Add(new TutoStep
        {
            message = "Ghosts can climb walls. Try climbing a wall by walking into it.",
            condition = () =>
            {
                var sim = m_ghost.GetComponent<GhostSimulateMovement>();
                if (sim != null && sim.m_isClimbing) hasClimbed = true;
                return hasClimbed;
            }
        });

        bool hasDashed = false;
        m_steps.Add(new TutoStep
        {
            message = "Dash with [{Ghost.Dash}] to move quickly.\nDash recharges after 20s or instantly on a successful sabotage.",
            condition = () =>
            {
                if (m_ghost.m_isDashing) hasDashed = true;
                return hasDashed;
            }
        });

        m_steps.Add(new TutoStep
        {
            message = "Sabotage the highlighted object with [{Ghost.Interact}], use [{Ghost.Validate}] to validate.\nThis will increase the sabotage bar over time.",
            onEnter = () => m_sabotageObject?.SetSabotable(true),
            condition = () => m_sabotageObject != null && m_sabotageObject.m_isSabotaged
        });

        m_steps.Add(new TutoStep
        {
            message = "Scan 3 highlighted objects with [{Ghost.Scan}]",
            onEnter = () => { SetScanObjectsEnabled(true); SetScanOutline(true); },
            condition = () =>
            {
                var wheel = m_ghost.GetComponent<GhostClientController>()?.m_wheel;
                if (wheel == null) return false;
                return wheel.m_wheelButtons.Count(b => !b.IsEmpty()) >= 3;
            }
        });

        m_steps.Add(new TutoStep
        {
            message = "Open the wheel by pressing and holding [{Ghost.OpenProps}], select a transformation by hovering over it and releasing the key.",
            onEnter = () =>
            {
                SetScanObjectsEnabled(false);
                SetScanOutline(false);
                var wheel = m_ghost.GetComponent<GhostClientController>()?.m_wheel;
                if (wheel != null) wheel.m_selectedPrefab = null;
            },
            condition = () =>
            {
                var wheel = m_ghost.GetComponent<GhostClientController>()?.m_wheel;
                return wheel != null && wheel.m_selectedPrefab != null;
            }
        });

        bool pressedLeft = false, pressedRight = false;
        m_steps.Add(new TutoStep
        {
            message = "Rotate the preview using [{Ghost.RotatePreviewLeft}] and [{Ghost.RotatePreviewRight}].",
            onEnter = () =>
            {
                var gc = m_ghost.GetComponent<GhostClientController>();
                if (gc != null) gc.m_morphBlocked = true;
            },
            condition = () =>
            {
                PlayerInput input = m_ghost.GetComponent<PlayerInput>();
                if (input == null) return false;
                if (input.actions["RotatePreviewLeft"].IsPressed())  pressedLeft  = true;
                if (input.actions["RotatePreviewRight"].IsPressed()) pressedRight = true;
                return pressedLeft && pressedRight;
            }
        });

        m_steps.Add(new TutoStep
        {
            message = "Confirm the transformation with [{Ghost.TransformConfirm}] to hide yourself.",
            onEnter = () =>
            {
                SetScanOutline(false);
                var gc = m_ghost.GetComponent<GhostClientController>();
                if (gc != null) gc.m_morphBlocked = false;
            },
            condition = () => m_ghostMorph != null && m_ghostMorph.m_isMorphed
        });


        m_steps.Add(new TutoStep
        {
            message = "Revive the ghost on the ground by pressing and holding [{Ghost.Interact}].",
            onEnter = () =>
            {
                m_ghostTuto.m_isStopped = true;
                SetRenderingOutline(m_ghostTuto.gameObject, "Outline_1", true);
            },
            condition = () => m_ghostTuto != null && !m_ghostTuto.m_isStopped
        });

        // CHILD PHASE
        m_childPhaseStart = m_steps.Count;

        m_steps.Add(new TutoStep
        {
            message = "Repair the sabotage with [{Child.Interact}], use [{Child.Validate}] to validate.",
            condition = () => m_sabotageObject != null && !m_sabotageObject.m_isSabotaged
        });

        bool hasSneaked = false;
        m_steps.Add(new TutoStep
        {
            message = "Sneak with [{Child.Sneak}] to move silently.",
            condition = () =>
            {
                if (!hasSneaked)
                {
                    PlayerInput input = m_child.GetComponent<PlayerInput>();
                    if (input != null && input.actions["Sneak"].IsPressed()) hasSneaked = true;
                }
                return hasSneaked;
            }
        });

        m_steps.Add(new TutoStep
        {
            message = "Hit objects with [{Child.Attack}].\nWarning: it costs money!",
            onEnter = () =>
            {
                SetBrokeDecorEnabled(true);
                SetScanOutline(true);
            },
            condition = () => m_scanObjects != null && m_scanObjects.Any(obj =>
            {
                if (obj == null) return false;
                BrokeDecor bd = obj.GetComponentInChildren<BrokeDecor>();
                return bd != null && bd.m_isBroken;
            })
        });

        bool m_initialRanged = false;
        m_steps.Add(new TutoStep
        {
            message = "Change weapon with [{Child.Change_weapon}].",
            onEnter = () =>
            {
                SetScanOutline(false);
                m_initialRanged = m_child.m_isRanged;
                var cc = m_child.GetComponent<ChildClientController>();
                if (cc != null) cc.m_weaponSwapBlocked = false;
            },
            condition = () => m_child.m_isRanged != m_initialRanged
        });

        m_steps.Add(new TutoStep
        {
            message = "A ghost is hidden in a mop, shoot him to untransform him.",
            onEnter = () => { UntransformGhostTuto();},
            condition = () => VerifieUntransform()
        });

        bool m_initialRanged2 = false;
        m_steps.Add(new TutoStep
        {
            message = "Shoot the ghost to slow it down with [{Child.Attack}].",
            onEnter = () =>
            {
                m_instance.SetActive(false);
                m_initialRanged2 = m_child.m_isRanged;
                var cc = m_child.GetComponent<ChildClientController>();
                if (cc != null) cc.m_weaponSwapBlocked = true;
            },
            condition = () => m_ghostTuto.m_isSlowed
        });

        m_steps.Add(new TutoStep
        {
            message = "Switch back to melee weapon with [{Child.Change_weapon}].",
            onEnter = () =>
            {
                m_instance.SetActive(false);
                var cc = m_child.GetComponent<ChildClientController>();
                if (cc != null) cc.m_weaponSwapBlocked = false;
            },
            condition = () => m_child.m_isRanged != m_initialRanged2
        });

        m_steps.Add(new TutoStep
        {
            message = "Knock out the ghost with [{Child.Attack}].\nWarning: if the ghost touches you, you'll be scared and unable to attack!",
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

        var cc = m_child.GetComponent<ChildClientController>();
        if (cc != null) cc.m_weaponSwapBlocked = true;

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

    private void SetBrokeDecorEnabled(bool _active) =>
        ForEachScanObject(obj => { var bd = obj.GetComponentInChildren<BrokeDecor>(); if (bd) bd.enabled = _active; });

    private void SetRenderingOutline(GameObject _target, string _layerName, bool _active)
    {
        uint layer = RenderingLayerMask.GetMask(_layerName);
        foreach (Renderer r in _target.GetComponentsInChildren<Renderer>())
        {
            if (_active) r.renderingLayerMask |= layer;
            else r.renderingLayerMask &= ~layer;
        }
    }

    //input binding helper

    private string ProcessBindings(string _message)
    {
        return System.Text.RegularExpressions.Regex.Replace(_message, @"\{(.*?)\}", match =>
        {
            string[] parts = match.Groups[1].Value.Split('.');
            if (parts.Length != 2) return match.Value;
            return InputBindingHelper.GetKey(parts[0], parts[1]);
        });
    }
}
