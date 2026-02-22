using PurrNet;
using UnityEngine;

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
    [SerializeField] private Renderer m_highlightRenderer;
    [SerializeField] private int m_highlightMaterialIndex = 0;
    [SerializeField] private Color m_emissionColor = new Color(0f, 1f, 1f, 1f);
    [SerializeField] private float m_emissionIntensity = 0.5f;

    [Header("Interaction")]
    [SerializeField] private string m_promptMessage = "E : Sabotage";
    [SerializeField] private GhostInteract m_saboteur;

    public bool m_isSabotaged;
    private bool m_isQteRunning;
    private bool m_isFocused;

    protected override void OnSpawned()
    {
        base.OnSpawned();
    }

    private void Start()
    {
        ApplyState();
        SetHighlight(false);
    }

    /**
    @brief      Focus : activates highlight + prompt
    @param      _playerType: player's type
    */
    
    public void OnFocus()
    {
        m_isFocused = true;
        SetHighlight(true);

        if (InteractPromptUI.m_Instance != null)
            InteractPromptUI.m_Instance.Show(m_promptMessage);
    }

    /**
    @brief      Unfocus : hides highlight + prompt
    @param      _playerType: player's type
    */
    public void OnUnfocus()
    {
        m_isFocused = false;

        SetHighlight(false);

        if (InteractPromptUI.m_Instance != null)
            InteractPromptUI.m_Instance.Hide();
    }

    public void OnInteract(GhostInteract _ghost)
    {
        if (m_isSabotaged || m_isQteRunning) return;
        Rigidbody rb = _ghost.GetComponentInParent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeAll;
        StartQte(_ghost);
    }

    public void OnStopInteract(GhostInteract _ghost)
    {

    }

    public void StartQte(GhostInteract _sabo)
    {
        m_isQteRunning = true;

        SetHighlight(false);
        if (InteractPromptUI.m_Instance != null)
            InteractPromptUI.m_Instance.Hide();

        m_saboteur = _sabo;
        QteCircle qte = FindAnyObjectByType<QteCircle>();
        qte.StartQte(OnQteFinished);
    }

    private void OnQteFinished(bool _success)
    {
        m_isQteRunning = false;

        m_saboteur.OnSabotageOver(_success);
        if (_success)
        {
            SabotageRPC();
            m_saboteur = null;
            return;
        }
        m_saboteur = null;

        if (m_isFocused)
        {
            SetHighlight(true);
            if (InteractPromptUI.m_Instance != null)
                InteractPromptUI.m_Instance.Show(m_promptMessage);
        }
    }

    [ServerRpc(requireOwnership:false)]
    private void SabotageRPC()
    {
        SabotageForAll();
    }

    [ObserversRpc(runLocally:true, requireServer:true)]
    private void SabotageForAll()
    {
        m_isSabotaged = true;

        ApplyState();
        SetHighlight(false);

        if (InteractPromptUI.m_Instance != null)
            InteractPromptUI.m_Instance.Hide();

        if (ScoreManager.m_Instance != null)
            ScoreManager.m_Instance.Add(m_scoreValue);
    }

    private void ApplyState()
    {
        if (m_normalMesh != null) m_normalMesh.SetActive(!m_isSabotaged);
        if (m_sabotagedMesh != null) m_sabotagedMesh.SetActive(m_isSabotaged);
    }

    private void SetHighlight(bool _enabled)
    {
        if (m_highlightRenderer == null) return;

        Material[] materials = m_highlightRenderer.materials;
        if (materials == null) return;

        int materialIndex = m_highlightMaterialIndex;
        if (materialIndex < 0 || materialIndex >= materials.Length) return;

        Material material = materials[materialIndex];
        if (material == null) return;
        if (!material.HasProperty("_EmissionColor")) return;

        if (_enabled)
        {
            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            material.SetColor("_EmissionColor", m_emissionColor * m_emissionIntensity);
        }
        else
        {
            material.SetColor("_EmissionColor", Color.black);
        }
    }
}
