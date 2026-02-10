using UnityEngine;

/**
@brief       Script du sabotage d'objet
@details     La classe \c SabotageObject gère le sabotage (QTE, meshes, highlight, score) et expose une interaction
             contrôlée par le joueur via \c IPlayerInteractable.
*/
public class SabotageObject : MonoBehaviour
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

    [Header("QTE")]
    [SerializeField] private QteCircle m_qteCircle;

    public bool m_isSabotaged;
    private bool m_isQteRunning;
    private bool m_isFocused;

    private void Start()
    {
        ApplyState();
        SetHighlight(false);
    }

    /**
    @brief      Focus : active highlight + prompt
    @param      _playerType: type du joueur
    */
    
    public void OnFocus()
    {
        m_isFocused = true;

        SetHighlight(true);

        if (InteractPromptUI.Instance != null)
            InteractPromptUI.Instance.Show(m_promptMessage);
    }

    /**
    @brief      Unfocus : coupe highlight + prompt
    @param      _playerType: type du joueur
    */
    public void OnUnfocus()
    {
        m_isFocused = false;

        SetHighlight(false);

        if (InteractPromptUI.Instance != null)
            InteractPromptUI.Instance.Hide();
    }

    public void StartQte(GhostInteract sabo)
    {
        if (m_qteCircle == null)
        {
            Sabotage();
            return;
        }

        m_isQteRunning = true;

        SetHighlight(false);
        if (InteractPromptUI.Instance != null)
            InteractPromptUI.Instance.Hide();

        m_saboteur = sabo;
        m_qteCircle.StartQte(OnQteFinished);
    }

    private void OnQteFinished(bool _success)
    {

        m_isQteRunning = false;

        m_saboteur.OnSabotageover(_success);
        if (_success)
        {
            Sabotage();
            m_saboteur = null;
            return;
        }
        m_saboteur = null;

        if (m_isFocused)
        {
            SetHighlight(true);
            if (InteractPromptUI.Instance != null)
                InteractPromptUI.Instance.Show(m_promptMessage);
        }
    }

    private void Sabotage()
    {
        m_isSabotaged = true;

        ApplyState();
        SetHighlight(false);

        if (InteractPromptUI.Instance != null)
            InteractPromptUI.Instance.Hide();

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.Add(m_scoreValue);
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
