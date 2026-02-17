using PurrNet;
using UnityEngine;
using System.Collections;

public class SabotageObject : NetworkBehaviour, IInteractable
{
    [Header("State Meshes")]
    [SerializeField] private GameObject m_normalMesh;
    [SerializeField] private GameObject m_sabotagedMesh;

    [Header("Score")]
    [SerializeField] private int m_scoreValue = 1;

    [Header("Highlight")]
    [SerializeField] private Renderer m_highlightRenderer;
    [SerializeField] private Color m_highlightColor = new Color(0f, 1f, 1f, 1f);
    [SerializeField] private float m_pulseSpeed = 3f;
    [SerializeField] private float m_minIntensity = 0.2f;
    [SerializeField] private float m_maxIntensity = 0.6f;

    [Header("Interaction")]
    [SerializeField] private string m_promptMessage = "E : Sabotage";
    [SerializeField] private GhostInteract m_saboteur;

    public QteCircle m_qteCircle;

    public bool m_isSabotaged;
    private bool m_isQteRunning;
    private bool m_isFocused;

    private Coroutine m_pulseCoroutine;
    private MaterialPropertyBlock m_propertyBlock;

    protected override void OnSpawned()
    {
        base.OnSpawned();
    }

    private void Start()
    {
        m_propertyBlock = new MaterialPropertyBlock();

        // Active l'émission sur les sharedMaterials une seule fois (comme GhostMorph)
        if (m_highlightRenderer != null)
        {
            foreach (Material mat in m_highlightRenderer.sharedMaterials)
            {
                if (mat != null)
                    mat.EnableKeyword("_EMISSION");
            }
        }

        ApplyState();
        SetHighlight(false);

        m_qteCircle = FindAnyObjectByType<QteCircle>();
    }

    public void OnFocus()
    {
        m_isFocused = true;
        SetHighlight(true);

        if (InteractPromptUI.m_Instance != null)
            InteractPromptUI.m_Instance.Show(m_promptMessage);
    }

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

    public void OnStopInteract(GhostInteract _ghost) { }

    public void StartQte(GhostInteract _sabo)
    {
        m_isQteRunning = true;
        SetHighlight(false);

        if (InteractPromptUI.m_Instance != null)
            InteractPromptUI.m_Instance.Hide();

        m_saboteur = _sabo;
        m_qteCircle.StartQte(OnQteFinished);
    }

    private void OnQteFinished(bool _success)
    {
        m_isQteRunning = false;

        m_saboteur.OnSabotageOver(_success);
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
            if (InteractPromptUI.m_Instance != null)
                InteractPromptUI.m_Instance.Show(m_promptMessage);
        }
    }

    private void Sabotage()
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

        if (_enabled)
        {
            if (m_pulseCoroutine != null)
                StopCoroutine(m_pulseCoroutine);
            m_pulseCoroutine = StartCoroutine(PulseHighlight());
        }
        else
        {
            if (m_pulseCoroutine != null)
            {
                StopCoroutine(m_pulseCoroutine);
                m_pulseCoroutine = null;
            }

            // Réinitialise la couleur d'émission via le PropertyBlock
            m_highlightRenderer.GetPropertyBlock(m_propertyBlock);
            m_propertyBlock.SetColor("_EmissionColor", Color.black);
            m_highlightRenderer.SetPropertyBlock(m_propertyBlock);
        }
    }

    private IEnumerator PulseHighlight()
    {
        float time = 0f;

        while (true)
        {
            float pulse = Mathf.Lerp(m_minIntensity, m_maxIntensity,
                                     (Mathf.Sin(time * m_pulseSpeed) + 1f) * 0.5f);

            m_highlightRenderer.GetPropertyBlock(m_propertyBlock);
            m_propertyBlock.SetColor("_EmissionColor", m_highlightColor * pulse);
            m_highlightRenderer.SetPropertyBlock(m_propertyBlock);

            time += Time.deltaTime;
            yield return null;
        }
    }
}
