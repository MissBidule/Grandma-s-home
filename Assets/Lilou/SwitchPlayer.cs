using UnityEngine;

/**
@brief       Permet de switcher entre Child et Ghost au clavier
@details     Active/désactive les composants nécessaires (Behavior + Movement) selon le PlayerType.
*/
public class SwitchPlayer : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private KeyCode m_switchKey = KeyCode.F1;

    [Header("References")]
    [SerializeField] private PlayerBehavior m_playerBehavior;
    [SerializeField] private ChildBehavior m_childBehavior;
    [SerializeField] private GhostBehavior m_ghostBehavior;
    [SerializeField] private ChildMovement m_childMovement;
    [SerializeField] private GhostMovement m_ghostMovement;

    [Header("Cameras")]
    [SerializeField] private GameObject m_childCameraRoot;
    [SerializeField] private GameObject m_ghostCameraRoot;

    private void Reset()
    {
        m_playerBehavior = GetComponent<PlayerBehavior>();
        m_childBehavior = GetComponent<ChildBehavior>();
        m_ghostBehavior = GetComponent<GhostBehavior>();
        m_childMovement = GetComponent<ChildMovement>();
        m_ghostMovement = GetComponent<GhostMovement>();
    }

    private void Awake()
    {
        if (m_playerBehavior == null) m_playerBehavior = GetComponent<PlayerBehavior>();
        if (m_childBehavior == null) m_childBehavior = GetComponent<ChildBehavior>();
        if (m_ghostBehavior == null) m_ghostBehavior = GetComponent<GhostBehavior>();
        if (m_childMovement == null) m_childMovement = GetComponent<ChildMovement>();
        if (m_ghostMovement == null) m_ghostMovement = GetComponent<GhostMovement>();
        // ⚠️ Ne pas forcer les caméras ici
    }

    private void Start()
    {
        ApplyPlayerType(m_playerBehavior != null ? m_playerBehavior.m_playerType : PlayerType.Child);
    }

    private void Update()
    {
        if (Input.GetKeyDown(m_switchKey))
        {
            Toggle();
        }
    }

    /**
    @brief      Alterne Child <-> Ghost
    @return     void
    */
    private void Toggle()
    {
        if (m_playerBehavior == null) return;

        PlayerType nextType = (m_playerBehavior.m_playerType == PlayerType.Child)
            ? PlayerType.Ghost
            : PlayerType.Child;

        ApplyPlayerType(nextType);
    }

    /**
    @brief      Applique le type (enable/disable scripts + layer + camera)
    @param      _playerType: type à appliquer
    @return     void
    */
    private void ApplyPlayerType(PlayerType _playerType)
    {
        if (m_playerBehavior != null)
            m_playerBehavior.m_playerType = _playerType;

        bool isChild = _playerType == PlayerType.Child;

        if (m_childBehavior != null) m_childBehavior.enabled = isChild;
        if (m_childMovement != null) m_childMovement.enabled = isChild;

        if (m_ghostBehavior != null) m_ghostBehavior.enabled = !isChild;
        if (m_ghostMovement != null) m_ghostMovement.enabled = !isChild;

        gameObject.layer = LayerMask.NameToLayer(isChild ? "Child" : "Ghost");

        // Caméras : une seule active à la fois
        if (m_childCameraRoot != null) m_childCameraRoot.SetActive(isChild);
        if (m_ghostCameraRoot != null) m_ghostCameraRoot.SetActive(!isChild);

        // Setup (si nécessaire)
        if (isChild && m_childBehavior != null) m_childBehavior.Setup();
        if (!isChild && m_ghostBehavior != null) m_ghostBehavior.Setup();
    }
}
