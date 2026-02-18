using PurrNet;
using UnityEngine;
using System.Collections;

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

    /*
     * @brief Initialises the material property block, enables emission keyword on the highlight renderer,
     *        applies the current state and retrieves the QteCircle instance
     * @return void
     */
    private void Start()
    {
        m_propertyBlock = new MaterialPropertyBlock();

        if (m_highlightRenderer != null)
        {
            foreach (Material mat in m_highlightRenderer.sharedMaterials)
            {
                if (mat != null)
                {
                    mat.EnableKeyword("_EMISSION");
                }
            }
        }

        ApplyState();
        SetHighlight(false);

        m_qteCircle = FindAnyObjectByType<QteCircle>();
    }

    /*
     * @brief Activates the highlight effect and displays the interaction prompt
     * @return void
     */
    public void OnFocus()
    {
        m_isFocused = true;
        SetHighlight(true);

        if (InteractPromptUI.m_Instance != null)
        {
            InteractPromptUI.m_Instance.Show(m_promptMessage);
        }
    }

    /*
     * @brief Deactivates the highlight effect and hides the interaction prompt
     * @return void
     */
    public void OnUnfocus()
    {
        m_isFocused = false;
        SetHighlight(false);

        if (InteractPromptUI.m_Instance != null)
        {
            InteractPromptUI.m_Instance.Hide();
        }
    }

    /*
     * @brief Handles player interaction with the sabotage object
     * Freezes the interacting ghost's rigidbody and starts the QTE if the object is not already sabotaged or busy
     * @param _ghost: The GhostInteract component of the interacting player
     * @return void
     */
    public void OnInteract(GhostInteract _ghost)
    {
        if (m_isSabotaged || m_isQteRunning)
        {
            return;
        }
        Rigidbody rb = _ghost.GetComponentInParent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeAll;
        StartQte(_ghost);
    }

    public void OnStopInteract(GhostInteract _ghost) { }

    /*
     * @brief Starts the QTE sequence for the given ghost interactor
     * Disables the highlight, hides the prompt and launches the QTE circle
     * @param _sabo: The GhostInteract component of the saboteur
     * @return void
     */
    public void StartQte(GhostInteract _sabo)
    {
        m_isQteRunning = true;
        SetHighlight(false);

        if (InteractPromptUI.m_Instance != null)
        {
            InteractPromptUI.m_Instance.Hide();
        }

        m_saboteur = _sabo;
        m_qteCircle.StartQte(OnQteFinished);
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
            Sabotage();
            m_saboteur = null;
            return;
        }
        m_saboteur = null;

        if (m_isFocused)
        {
            SetHighlight(true);
            if (InteractPromptUI.m_Instance != null)
            {
                InteractPromptUI.m_Instance.Show(m_promptMessage);
            }
        }
    }

    /*
     * @brief Marks the object as sabotaged, updates its visual state and increments the score
     * @return void
     */
    private void Sabotage()
    {
        m_isSabotaged = true;
        ApplyState();
        SetHighlight(false);

        if (InteractPromptUI.m_Instance != null)
        {
            InteractPromptUI.m_Instance.Hide();
        }

        if (ScoreManager.m_Instance != null)
        {
            ScoreManager.m_Instance.Add(m_scoreValue);
        }
    }

    /*
     * @brief Toggles the normal and sabotaged meshes according to the current sabotage state
     * @return void
     */
    private void ApplyState()
    {
        if (m_normalMesh != null)
        {
            m_normalMesh.SetActive(!m_isSabotaged);
        }
        if (m_sabotagedMesh != null)
        {
            m_sabotagedMesh.SetActive(m_isSabotaged);
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
        if (m_highlightRenderer == null)
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

            m_highlightRenderer.GetPropertyBlock(m_propertyBlock);
            m_propertyBlock.SetColor("_EmissionColor", Color.black);
            m_highlightRenderer.SetPropertyBlock(m_propertyBlock);
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

            m_highlightRenderer.GetPropertyBlock(m_propertyBlock);
            m_propertyBlock.SetColor("_EmissionColor", m_highlightColor * pulse);
            m_highlightRenderer.SetPropertyBlock(m_propertyBlock);

            time += Time.deltaTime;
            yield return null;
        }
    }
}
