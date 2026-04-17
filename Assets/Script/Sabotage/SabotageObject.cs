using System.Collections.Generic;
using PurrNet;
using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;
using UnityEngine.SocialPlatforms.Impl;

/*
 * @brief  Contains class declaration for SabotageObject
 * @details Script that handles sabotage (QTE, meshes...) and exposes an interaction handled by the player
 */
public class SabotageObject : NetworkBehaviour, IInteractable
{
    [Header("Sabotaged VFX")]
    [SerializeField] private GameObject m_vfxPrefab;
    [SerializeField] private GameObject m_interactPrefab;
    private GameObject m_vfx;

    [Header("Score")]
    [SerializeField] private int m_scoreValue = 1;

    [Header("Highlight")]
    [SerializeField] private List<Renderer> m_highlightRenderers = new List<Renderer>();
    [SerializeField] private Color m_highlightColor = new Color(0f, 1f, 1f, 1f);
    [SerializeField] private RenderingLayerMask m_notSabotagedLayer;
    [SerializeField] private RenderingLayerMask m_sabotagedLayer;
    [SerializeField] private float m_pulseSpeed = 3f;
    [SerializeField] private float m_minIntensity = 0.2f;
    [SerializeField] private float m_maxIntensity = 0.6f;

    [Header("Interaction")]
    [SerializeField] private string m_promptLabelSABOTAGE = "Sabotage";
    [SerializeField] private string m_promptLabelREPAIR = "Repair";
    [SerializeField] private string m_promptLabelVALID = "Valid";
    [SerializeField] private float m_repairDelay = 10f;
    [SerializeField] private Interact m_saboteur;

    public bool m_isSabotaged;
    public bool m_isSabotable { get; private set; }
    private bool m_isQteRunning;
    private bool m_isFocused;
    private bool m_isPanicMode;

    [SerializeField] public List<Interact> m_saboteurs = new List<Interact>();
    private Coroutine m_pulseCoroutine;
    private MaterialPropertyBlock m_propertyBlock;

    protected override void OnSpawned()
    {
        base.OnSpawned();
    }

    /*
     * @brief Initialises the material property block, enables emission keyword on the highlight renderer,
     *        applies the current state and retrieves the QteCircle instance
     * @return void
     */
    private void Start()
    {
        m_propertyBlock = new MaterialPropertyBlock();
        m_highlightRenderers.Add(GetComponent<Renderer>());

        if (m_highlightRenderers.Count > 0)
        {
            foreach (Renderer r in m_highlightRenderers)
            {
                foreach (Material mat in r.sharedMaterials)
                {
                    if (mat != null)
                    {
                        mat.EnableKeyword("_EMISSION");
                    }
                }
            }
        }
        m_vfx = UnityProxy.Instantiate(m_vfxPrefab, transform);
        ApplyState();
        SetHighlight(false);
    }

    /*
     * @brief Activates the highlight effect and displays the interaction prompt
     * @return void
     */
    public void OnFocus(Interact _player)
    {
        m_isFocused = true;
        if (!m_isPanicMode)
        {
            if (!m_isSabotaged)
            {
                GhostMorph ghostMorph = _player.GetComponentInParent<GhostMorph>();
                bool isMorphed = ghostMorph != null && ghostMorph.m_isMorphed;
                bool canSabotage = _player.m_isGhost && m_isSabotable && !isMorphed;
                if (canSabotage) InteractPromptUI.m_Instance.ShowDynamic(() => InputBindingHelper.BuildPrompt("Ghost", "Interact", m_promptLabelSABOTAGE));
                SetHighlight(canSabotage);
            }
            if (m_isSabotaged && !_player.m_isGhost)
            {
                InteractPromptUI.m_Instance.ShowDynamic(() => InputBindingHelper.BuildPrompt("Child", "Interact", m_promptLabelREPAIR));
                SetHighlight(true);
            }
        }
        m_saboteurs.Add(_player);
    }

    /*
     * @brief Deactivates the highlight effect and hides the interaction prompt
     * @return void
     */
    public void OnUnfocus(Interact _player)
    {
        m_isFocused = false;

        m_saboteurs.Remove(_player);
        InteractPromptUI.m_Instance.Hide();
        SetHighlight(false);
    }
    /*
     * @brief Handles player interaction with the sabotage object
     * Freezes the interacting ghost's rigidbody and starts the QTE if the object is not already sabotaged or busy
     * @param _player: The Interact component of the interacting player
     * @return void
     */
    public void OnInteract(Interact _player)
    {
        if (m_isPanicMode) return;
        if (_player.m_isGhost && !m_isSabotable) return;
        if (m_isQteRunning && _player.m_isGhost)
        {
            if (_player != m_saboteur)
                StartCoroutine(ShowTempPrompt("<color=red>Already in use</color>", 2f));
            return;
        }
        if ((m_isSabotaged && _player.m_isGhost) || (!_player.m_isGhost && !m_isSabotaged))
            return;
        GhostMorph ghostMorph = _player.GetComponentInParent<GhostMorph>();
        if (ghostMorph != null && ghostMorph.m_isMorphed)
        {
            return;
        }
        ChildController childController = _player.GetComponentInParent<ChildController>();
        //if (childController != null && childController.m_isScared) return;
        ChildClientController childClientController;
        if (childClientController = _player.GetComponentInParent<ChildClientController>())
        {
            childClientController.RepairAnimation(true);
        }
        else
        {
            GhostClientController ghostClientController = _player.GetComponentInParent<GhostClientController>();
            ghostClientController.SabotageAnimation(true);
        }
            Rigidbody rb = _player.GetComponentInParent<Rigidbody>();
        rb.constraints = (RigidbodyConstraints)(RigidbodyConstraints.FreezeAll - RigidbodyConstraints.FreezePositionY);
        SetQteRunningServer(true);
        StartQte(_player);
        StartVfxForAll();
    }

    [ServerRpc(requireOwnership:false)]
    private void StartVfxForAll(RPCInfo info = default) => StartVfxForAllObservers();

    [ObserversRpc(runLocally:true, requireServer:true)]
    private void StartVfxForAllObservers() => m_interactPrefab.SetActive(true);

    public void OnStopInteract(Interact _player) { }

    [ServerRpc(requireOwnership:false)]
    private void StopVfxForAll(RPCInfo info = default) => StopVfxForAllObservers();

    [ObserversRpc(runLocally:true, requireServer:true)]
    private void StopVfxForAllObservers() => m_interactPrefab.SetActive(false);

    private IEnumerator ShowTempPrompt(string _message, float _duration)
    {
        InteractPromptUI.m_Instance.Show(_message);
        yield return new WaitForSeconds(_duration);
        InteractPromptUI.m_Instance.Hide();
    }

    [ServerRpc(requireOwnership: false)]
    private void SetQteRunningServer(bool _running)
    {
        SetQteRunningForAll(_running);
    }

    [ObserversRpc(runLocally: true, requireServer: true)]
    private void SetQteRunningForAll(bool _running)
    {
        m_isQteRunning = _running;
    }

    /*
     * @brief Starts the QTE sequence for the given ghost interactor
     * Disables the highlight, hides the prompt and launches the QTE circle
     * @param _sabo: The Interact component of the saboteur
     * @return void
     */
    public void StartQte(Interact _sabo)
    {
        m_interactPrefab.SetActive(true);
        m_isQteRunning = true;
        SetHighlight(false);

        string validMap = _sabo.m_isGhost ? "Ghost" : "Child";
        InteractPromptUI.m_Instance.ShowDynamic(() => InputBindingHelper.BuildPrompt(validMap, "Validate", m_promptLabelVALID));

        m_saboteur = _sabo;
        QteCircle qte = FindAnyObjectByType<QteCircle>();
        qte.StartQte(OnQteFinished);
    }

    /*
     * @brief Callback triggered when the QTE ends
     * Applies sabotage on success or restores the highlight and prompt on failure
     * @param _success: Whether the player successfully completed the QTE
     * @return void
     */
    private void OnQteFinished(bool _success)
    {
        StopVfxForAll();
        m_isQteRunning = false;

        m_saboteur.OnSabotageOver(_success);

        ChildClientController childClientController = m_saboteur.GetComponentInParent<ChildClientController>();
        if (childClientController != null)
        {
            childClientController.RepairAnimation(false);
        }
        else
        {
            GhostClientController ghostClientController = m_saboteur.GetComponentInParent<GhostClientController>();
            ghostClientController.SabotageAnimation(false);
        }
        if (_success)
        {
            InteractPromptUI.m_Instance.Hide();

            m_saboteur.OnSuccessSabotage();
            if (m_saboteur.m_isGhost)
            {
                SabotageRPC();
            }
            else
            {
                UnsabotageRPC();
            }

            m_saboteur = null;
            return;
        }
        else if (!m_isPanicMode)
        {
            bool isGhost = m_saboteur.m_isGhost;
            InteractPromptUI.m_Instance.ShowDynamic(() => isGhost
                ? InputBindingHelper.BuildPrompt("Ghost", "Interact", m_promptLabelSABOTAGE)
                : InputBindingHelper.BuildPrompt("Child", "Interact", m_promptLabelREPAIR));
        }

        m_saboteur = null;
        SetQteRunningServer(false);

        if (m_isFocused && !m_isPanicMode)
        {
            SetHighlight(true);
        }
    }

// le [] sert vraiment? a verifier
    [ServerRpc(requireOwnership:false)]
    private void SabotageRPC(RPCInfo info = default)
    {
        if (m_isSabotaged) return;
        SabotageForAll();
        if(InstanceHandler.TryGetInstance(out ScoreManager scoreManager))
        {
            scoreManager.AddPointSabotage(info.sender);
        }

    }

    [ObserversRpc(runLocally:true, requireServer:true)]
    private void SabotageForAll()
    {
        m_isSabotaged = true;
        m_repairEnabled = false;
        m_interactPrefab.SetActive(false);
        ApplyState();
        SetHighlight(false);
        StartCoroutine(EnableRepairAfterDelay());
    }

    private bool m_repairEnabled;

    private IEnumerator EnableRepairAfterDelay()
    {
        yield return new WaitForSeconds(m_repairDelay);
        m_repairEnabled = true;
        ApplyRenderingLayer();
        foreach (Interact player in m_saboteurs)
        {
            if (!player.m_isGhost)
            {
                InteractPromptUI.m_Instance.Show(InputBindingHelper.BuildPrompt("Child", "Interact", m_promptLabelREPAIR));
                SetHighlight(true);
                break;
            }
        }
    }

    [ServerRpc(requireOwnership:false)]
    private void UnsabotageRPC(RPCInfo info = default)
    {
        UnsabotageForAll();

        if(InstanceHandler.TryGetInstance(out ScoreManager scoreManager))
            scoreManager.SubPointSabotage(info.sender);

        SabotageManager sabotageManager = FindAnyObjectByType<SabotageManager>();
        sabotageManager?.OnObjectRepaired(this);
    }

    public void SetSabotable(bool _sabotable)
    {
        if (!isServer) return;
        SetSabotableForAll(_sabotable);
    }

    [ObserversRpc(runLocally:true, requireServer:true, bufferLast:true)]
    private void SetSabotableForAll(bool _sabotable)
    {
        m_isSabotable = _sabotable;
        ApplyState();
    }

    public void SetPanicMode()
    {
        if (!isServer) return;
        SetPanicModeForAll();
    }

    [ObserversRpc(runLocally:true, requireServer:true)]
    private void SetPanicModeForAll()
    {
        m_isPanicMode = true;
        SetHighlight(false);
        ApplyRenderingLayer();
        if (m_isQteRunning)
        {
            QteCircle qte = FindAnyObjectByType<QteCircle>();
            qte?.CancelQte();
        }
    }

    [ObserversRpc(runLocally:true, requireServer:true)]
    private void UnsabotageForAll()
    {
        m_isSabotaged = false;
        m_repairEnabled = false;
        m_interactPrefab.SetActive(false);
        ApplyState();
        SetHighlight(false);
    }

    /*
     * @brief Toggles the normal and sabotaged meshes according to the current sabotage state
     * @return void
     */
    private void ApplyState()
    {
        foreach (Interact interact in m_saboteurs)
            interact.OnSabotageOver(true);
        ApplyRenderingLayer();
    }

    private void ApplyRenderingLayer()
    {
        foreach (Renderer renderer in m_highlightRenderers)
        {
            renderer.renderingLayerMask = 0;
            RenderingLayerMask activeLayer = m_isPanicMode ? default : ((m_isSabotaged && m_repairEnabled) ? m_sabotagedLayer : (m_isSabotable && !m_isSabotaged ? m_notSabotagedLayer : default));
            renderer.renderingLayerMask |= activeLayer + (uint)RenderingLayerMask.defaultRenderingLayerMask;
        }
        if (m_vfx != null)
        {
            m_vfx.SetActive(m_isSabotaged);
        }
    }


    /*
     * @brief Starts or stops the pulsing highlight coroutine on the highlight renderer
     * Resets emission to black when disabled
     * @param _enabled: Whether the highlight should be active
     * @return void
     */
    private void SetHighlight(bool _enabled)
    {
        if (m_highlightRenderers.Count == 0)
        {
            return;
        }

        if (_enabled)
        {
            if (m_pulseCoroutine != null)
            {
                StopCoroutine(m_pulseCoroutine);
            }
            m_pulseCoroutine = StartCoroutine(PulseHighlight());
        }
        else
        {
            if (m_pulseCoroutine != null)
            {
                StopCoroutine(m_pulseCoroutine);
                m_pulseCoroutine = null;
            }


            foreach (Renderer r in m_highlightRenderers)
            {
                r.GetPropertyBlock(m_propertyBlock);
                m_propertyBlock.SetColor("_EmissionColor", Color.black);
                r.SetPropertyBlock(m_propertyBlock);
            }
        }
    }

    /*
     * @brief Animates the highlight renderer with a pulsing emission effect
     * @return IEnumerator for coroutine
     */
    private IEnumerator PulseHighlight()
    {
        float time = 0f;

        while (true)
        {
            float pulse = Mathf.Lerp(m_minIntensity, m_maxIntensity,
                                     (Mathf.Sin(time * m_pulseSpeed) + 1f) * 0.5f);

            foreach (Renderer r in m_highlightRenderers)
            {
                r.GetPropertyBlock(m_propertyBlock);
                m_propertyBlock.SetColor("_EmissionColor", m_highlightColor * pulse);
                r.SetPropertyBlock(m_propertyBlock);
            }

            time += Time.deltaTime;
            yield return null;
        }
    }
}
