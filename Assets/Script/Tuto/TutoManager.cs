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
    [SerializeField] private GhostController m_ghostTuto;
    [SerializeField] private ChildController m_child;

    [Header("Tutorial Objects")]
    [SerializeField] private SabotageObject m_sabotageObject;
    [SerializeField] private GameObject[] m_scanObjects;

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
            message = "Sabote l'objet en surbrillance avec {Ghost.Interact}, cela augmentera la jauge de sabotage au fil du temps",
            onEnter = () => m_sabotageObject?.SetSabotable(true),
            condition = () => m_sabotageObject != null && m_sabotageObject.m_isSabotaged
        });

        m_steps.Add(new TutoStep
        {
            message = "Scanne 3 objets en surbrillance avec {Ghost.Scan}",
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
            message = "Ouvre la roue en restant appuié sur {Ghost.OpenProps} et sélectionne une transformation en relachant la touche",
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
            message = "Confirme la transformation avec {Ghost.TransformConfirm} pour te cacher",
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
            message = "Réanime le fantôme à terre avec {Ghost.Interact}",
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
            message = "Répare le sabotage avec {Child.Interact}",
            onEnter = () =>
            {
                if (m_sabotageObject != null)
                    foreach (Outline o in m_sabotageObject.GetComponentsInChildren<Outline>())
                        o.enabled = true;
            },
            condition = () => m_sabotageObject != null && !m_sabotageObject.m_isSabotaged
        });

        m_steps.Add(new TutoStep
        {
            message = "Tape sur des objet avec {Child.Attack} \n Attention : ça coûte de l'argent !",
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
            message = "Change d'arme avec {Child.Change_weapon}",
            onEnter = () => m_initialRanged = m_child.m_isRanged,
            condition = () => m_child.m_isRanged != m_initialRanged
        });

        bool m_initialRanged2 = false;
        m_steps.Add(new TutoStep
        {
            message = "Tire sur le fantôme pour le ralentir avec {Child.Attack}",
            onEnter = () => { m_ghostTuto.gameObject.SetActive(true); SetRenderingOutline(m_ghostTuto.gameObject, "Outline_1", true); m_initialRanged2 = m_child.m_isRanged; },
            condition = () => m_ghostTuto.m_isSlowed
        });

        m_steps.Add(new TutoStep
        {
            message = "Repasse en arme au corps à corps avec {Child.Change_weapon}",
            condition = () => m_child.m_isRanged != m_initialRanged2
        });

        m_steps.Add(new TutoStep
        {
            message = "Assomme le fantôme avec {Child.Attack}",
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
