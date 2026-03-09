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
    [Header("State Meshes")]
    [SerializeField] private GameObject m_normalMesh;
    [SerializeField] private GameObject m_sabotagedMesh;

    [Header("Score")]
    [SerializeField] private int m_scoreValue = 1;

    [Header("Highlight")]
    [SerializeField] private List<Renderer> m_highlightRenderers = new List<Renderer>();
    [SerializeField] private Color m_highlightColor = new Color(0f, 1f, 1f, 1f);
    [SerializeField] private float m_pulseSpeed = 3f;
    [SerializeField] private float m_minIntensity = 0.2f;
    [SerializeField] private float m_maxIntensity = 0.6f;

    [Header("Interaction")]
    [SerializeField] private string m_promptMessageSABOTAGE = "E : Sabotage";
    [SerializeField] private string m_promptMessageREPAIR = "E : Repair";
    [SerializeField] private string m_promptMessageSPACE = "SPACE : Valid";
    [SerializeField] private Interact m_saboteur;

    public bool m_isSabotaged;
    private bool m_isQteRunning;
    private bool m_isFocused;

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
        if (m_sabotagedMesh != null)
        {
            if (!m_isSabotaged)
            {
                if (_player.m_isGhost) InteractPromptUI.m_Instance.Show(m_promptMessageSABOTAGE);
                else InteractPromptUI.m_Instance.Hide();
                SetHighlight(_player.m_isGhost);

                
            }
            if (m_isSabotaged)
            {
                if (_player.m_isGhost) InteractPromptUI.m_Instance.Hide();
                else InteractPromptUI.m_Instance.Show(m_promptMessageREPAIR);
                SetHighlight(!_player.m_isGhost);
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
        if ((m_isSabotaged && _player.m_isGhost) || (!_player.m_isGhost && !m_isSabotaged) || m_isQteRunning)
        {
            return;
        }
        Rigidbody rb = _player.GetComponentInParent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeAll;
        StartQte(_player);
    }

    public void OnStopInteract(Interact _player) { }

    /*
     * @brief Starts the QTE sequence for the given ghost interactor
     * Disables the highlight, hides the prompt and launches the QTE circle
     * @param _sabo: The Interact component of the saboteur
     * @return void
     */
    public void StartQte(Interact _sabo)
    {
        Debug.Log(_sabo.transform.parent.name + " started sabotage");
        m_isQteRunning = true;
        SetHighlight(false);

        InteractPromptUI.m_Instance.Show(m_promptMessageSPACE);
        
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
        m_isQteRunning = false;
        
        m_saboteur.OnSabotageOver(_success);
        if (_success)
        {
            InteractPromptUI.m_Instance.Hide();
            
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
        else
        {
            string prompt = m_saboteur.m_isGhost ? m_promptMessageSABOTAGE : m_promptMessageREPAIR;
            InteractPromptUI.m_Instance.Show(prompt);
        }
        
        m_saboteur = null;

        if (m_isFocused)
        {
            SetHighlight(true);

        }
    }

// le [] sert vraiment? a verifier
    [ServerRpc(requireOwnership:false)]
    private void SabotageRPC(RPCInfo info = default)
    {
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
        ApplyState();
        SetHighlight(false);
    }

    [ServerRpc(requireOwnership:false)]
    private void UnsabotageRPC()
    {
        UnsabotageForAll();
    }

    [ObserversRpc(runLocally:true, requireServer:true)]
    private void UnsabotageForAll()
    {
        m_isSabotaged = false;
        ApplyState();
        SetHighlight(false);
    }

    /*
     * @brief Toggles the normal and sabotaged meshes according to the current sabotage state
     * @return void
     */
    private void ApplyState()
    {
        if (m_normalMesh != null)
        {
            var r = m_normalMesh.GetComponent<Renderer>();
            if (r != null)
                r.enabled = !m_isSabotaged;

            foreach (Interact interact in m_saboteurs)
            {
                
                interact.OnSabotageOver( true);

                Debug.Log("iteration");       
            }
            var c = m_normalMesh.GetComponent<Collider>();
            if (c != null)
                c.enabled = !m_isSabotaged;
        }
        

        if (m_sabotagedMesh != null)
        {
            var r = m_sabotagedMesh.GetComponent<Renderer>();
            if (r != null)
                r.enabled = m_isSabotaged;
                
            var c = m_sabotagedMesh.GetComponent<Collider>();
            if (c != null)
                c.enabled = m_isSabotaged;
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
