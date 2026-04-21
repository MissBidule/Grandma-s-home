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
    [Header("Players")]
    [SerializeField] private GhostController m_ghost;
    [SerializeField] private ChildController m_child;

    [Header("Tutorial Objects")]
    [SerializeField] private SabotageObject m_sabotageObject;
    [SerializeField] private SabotageObject m_repairObject;
    [SerializeField] private BrokeDecor m_brokeDecor;
    [SerializeField] private GameObject[] m_scanObjects;
    [SerializeField] private Transform m_ghostTransformForChildTuto;

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
    BrokeDecor brokeDecor;

    // Called by TutoPlayerSpawningState after spawn
    public void Init(GhostController _ghost, ChildController _child)
    {
        m_ghost = _ghost;
        m_child = _child;

        m_ghostMorph = m_ghost.GetComponent<GhostMorph>();
        m_ghostMorphPreview = m_ghost.GetComponentInChildren<GhostMorphPreview>();
        brokeDecor =m_brokeDecor.GetComponent<BrokeDecor>();

        m_sabotageObject ??= FindAnyObjectByType<SabotageObject>();
        m_brokeDecor ??= FindAnyObjectByType<BrokeDecor>();

        m_sabotageObject?.SetSabotable(false);
        SetScanObjectsEnabled(false);
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
            m_ui.ShowText("Tuto terminé ! Appuie sur Échap pour quitter.");
            m_currentStep = m_steps.Count;
            return;
        }

        if (next == m_childPhaseStart)
        {
            m_waitingForFade = true;
            m_ui.HideText();
            m_ui.FadeAndSwitch(
                _onBlack: SwitchToChild,
                _onDone: () => { m_waitingForFade = false; EnterStep(next); m_ghost.transform.position = m_ghostTransformForChildTuto.position; }
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


    private void BuildSteps()
    {
        // GHOST PHASE
        bool movedForward = false, movedBack = false, movedLeft = false, movedRight = false;
        m_steps.Add(new TutoStep
        {
            message = "Déplace-toi avec Z Q S D dans la pièce",
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

        m_steps.Add(new TutoStep
        {
            message = "Sabote l'objet en surbrillance avec {Ghost.Interact}",
            onEnter = () => m_sabotageObject?.SetSabotable(true),
            condition = () => m_sabotageObject != null && m_sabotageObject.m_isSabotaged
        });

        m_steps.Add(new TutoStep
        {
            message = "Scanne 5 objets en surbrillance avec {Ghost.Scan}",
            onEnter = () => { SetScanObjectsEnabled(true); SetScanOutline(true); },
            condition = () =>
            {
                var wheel = m_ghost.GetComponent<GhostClientController>()?.m_wheel;
                if (wheel == null) return false;
                return wheel.m_wheelButtons.Count(b => !b.IsEmpty()) >= 5;
            }
        });

        m_steps.Add(new TutoStep
        {
            message = "Ouvre la roue en restant appuié sur {Ghost.OpenProps} et sélectionne une transformation en relachant la touche",
            onEnter = () =>
            {
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
            message = "Tourne la prévisualisation avec {Ghost.RotatePreviewLeft} / {Ghost.RotatePreviewRight}",
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
            message = "Transforme-toi avec {Ghost.TransformConfirm}",
            onEnter = () =>
            {
                SetScanOutline(false);
                var gc = m_ghost.GetComponent<GhostClientController>();
                if (gc != null) gc.m_morphBlocked = false;
            },
            condition = () => m_ghostMorph != null && m_ghostMorph.m_isMorphed
        });


        // CHILD PHASE
        m_childPhaseStart = m_steps.Count;

        m_steps.Add(new TutoStep
        {
            message = "Répare le sabotage avec {Child.Interact}",
            onEnter = () =>
            {
                if (m_repairObject != null)
                    foreach (Outline o in m_repairObject.GetComponentsInChildren<Outline>())
                        o.enabled = true;
            },
            condition = () => m_repairObject != null && !m_repairObject.m_isSabotaged
        });

        m_steps.Add(new TutoStep
        {
            message = "Frappe cet objet avec {Child.Attack}\nAttention : ça coûte de l'argent !",
            onEnter = () =>
            {
                brokeDecor.enabled=true;
            },
            condition = () => m_brokeDecor != null && m_brokeDecor.m_isBroken
        });

        bool m_initialRanged = false;
        m_steps.Add(new TutoStep
        {
            message = "Change d'arme avec {Child.Change_weapon}",
            onEnter = () => m_initialRanged = m_child.m_isRanged,
            condition = () => m_child.m_isRanged != m_initialRanged
        });

        m_steps.Add(new TutoStep
        {
            message = "Tire sur le fantôme pour le ralentir avec {Child.Attack}",
            condition = () => m_ghost.m_isSlowed
        });

       /* m_steps.Add(new TutoStep
        {
            message = "Réveille le fantôme à terre avec {Child.Interact}",
            onEnter = () => m_ghost.ApplyStopToAll(),
            condition = () => !m_ghost.m_isStopped
        });*/
    }

    // POV switch

    private void SwitchToChild()
    {
        m_child.gameObject.SetActive(true);
        SetPlayerActive(m_ghost.gameObject, false);
        SetPlayerActive(m_child.gameObject, true);

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

    // scan helpers

    private void SetScanObjectsEnabled(bool _active)
    {
        if (m_scanObjects == null) return;
        foreach (GameObject obj in m_scanObjects)
        {
            if (obj == null) continue;
            ScannableObject s = obj.GetComponentInChildren<ScannableObject>();
            if (s == null) continue;
            s.m_isScannable = _active;
        }
    }

    private void SetScanOutline(bool _active)
    {
        if (m_scanObjects == null) return;
        foreach (GameObject obj in m_scanObjects)
        {
            if (obj == null) continue;
            foreach (Outline outline in obj.GetComponentsInChildren<Outline>())
                outline.enabled = _active;
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
