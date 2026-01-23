using UnityEngine;

/**
@brief       Script du sabotage d'objet
@details     La classe \c SabotageObject gère l'interaction de sabotage : proximité, surbrillance (émission),
             swap d'état, ajout de score et QTE.
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
    [SerializeField] private KeyCode m_interactKey = KeyCode.E;

    [Header("QTE")]
    [SerializeField] private QteCircle m_qteCircle;

    private bool m_isPlayerInRange;
    private bool m_isSabotaged;
    private bool m_isQteRunning;

    /**
    @brief      Initialise l'état visuel et coupe l'émission
    @return     void
    */
    private void Start()
    {
        ApplyState();
        SetHighlight(false);
    }

    /**
    @brief      Gère l'interaction au clavier.
    @return     void
    */
    private void Update()
    {
        if (!m_isPlayerInRange) return;
        if (m_isSabotaged) return;
        if (m_isQteRunning) return;

        if (Input.GetKeyDown(m_interactKey))
        {
            StartQte();
        }
    }

    /**
    @brief      Lance le QTE de sabotage.
    @return     void
    */
    private void StartQte()
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

        m_qteCircle.StartQte(OnQteFinished);
    }

    /**
    @brief      Callback appelée à la fin du QTE.
    @param      _success: true si le QTE est réussi
    @return     void
    */
    private void OnQteFinished(bool _success)
    {
        m_isQteRunning = false;

        if (!m_isPlayerInRange || m_isSabotaged) return;

        if (_success)
        {
            Sabotage();
        }
        else
        {
            SetHighlight(true);
            if (InteractPromptUI.Instance != null)
                InteractPromptUI.Instance.Show(m_promptMessage);
        }
    }

    /**
    @brief      Passe l'objet en état saboté + score
    @return     void
    */
    private void Sabotage()
    {
        m_isSabotaged = true;

        ApplyState();
        SetHighlight(false);

        if (InteractPromptUI.Instance != null)
            InteractPromptUI.Instance.Hide();

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.Add(m_scoreValue);
        }
    }

    /**
    @brief      Active/désactive les meshes selon l'état
    @return     void
    */
    private void ApplyState()
    {
        if (m_normalMesh != null) m_normalMesh.SetActive(!m_isSabotaged);
        if (m_sabotagedMesh != null) m_sabotagedMesh.SetActive(m_isSabotaged);
    }

    /**
    @brief      Active/Désactive l'émission uniquement sur le matériau ciblé (bois)
    @param      _enabled: true active la surbrillance, false la coupe
    @return     void
    */
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

    /**
    @brief      Détecte l'entrée du joueur et active la surbrillance
    @param      _other: collider entrant
    @return     void
    */
    private void OnTriggerEnter(Collider _other)
    {
        if (!_other.CompareTag("Player")) return;

        m_isPlayerInRange = true;

        if (!m_isSabotaged && !m_isQteRunning)
        {
            SetHighlight(true);
            if (InteractPromptUI.Instance != null)
                InteractPromptUI.Instance.Show(m_promptMessage);
        }
    }

    /**
    @brief      Détecte la sortie du joueur et coupe la surbrillance
    @param      _other: collider sortant
    @return     void
    */
    private void OnTriggerExit(Collider _other)
    {
        if (!_other.CompareTag("Player")) return;

        m_isPlayerInRange = false;

        SetHighlight(false);
        if (InteractPromptUI.Instance != null)
            InteractPromptUI.Instance.Hide();
    }
}
