using UnityEngine;
using UnityEngine.UI;

/**
@brief       Displays a death indicator above a ghost's head when stopped
@details     Visible only to ghost players, renders through walls.
             Attach to the Ghost prefab alongside GhostController.
             Requires a child Canvas (World Space) with an Image as the indicator.
*/
public class GhostDeathIndicator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Canvas m_indicatorCanvas;

    private GhostController m_ghostController;
    private bool m_isLocalPlayerGhost;
    private bool m_initialized;
    private float m_deathTimer;
    private bool m_isDying;

    private void Start()
    {
        m_ghostController = GetComponent<GhostController>();

        // Set all UI graphics to render through walls (ZTest Always)
        foreach (var graphic in m_indicatorCanvas.GetComponentsInChildren<Graphic>(true))
        {
            Material mat = new Material(graphic.material);
            mat.SetInt("unity_GUIZTestMode", (int)UnityEngine.Rendering.CompareFunction.Always);
            graphic.material = mat;
        }

        m_indicatorCanvas.gameObject.SetActive(false);
    }

    /**
    @brief      Called by GhostStatus ObserversRpc when ghost is killed
    */
    public void OnGhostDied()
    {
        m_isDying = true;
        m_deathTimer = m_ghostController.m_timerStop;
    }

    private void LateUpdate()
    {
        if (!m_initialized)
        {
            GameObject localPlayer = GameObject.FindWithTag("Player");
            if (localPlayer != null)
            {
                m_isLocalPlayerGhost = localPlayer.GetComponent<GhostController>() != null;
                m_initialized = true;
            }
            else return;
        }

        if (m_isDying)
        {
            m_deathTimer -= Time.deltaTime;

            // Hide when timer expires or ghost revived early (owner client)
            if (m_deathTimer <= 0f || !m_ghostController.m_isStopped)
            {
                m_isDying = false;
                // Keep state consistent on non-owner clients where GhostController.Update does not run
                m_ghostController.m_isStopped = false;
                m_ghostController.m_stoppedLabel.SetActive(false);
            }
        }

        bool shouldShow = m_isLocalPlayerGhost && m_isDying;
        m_indicatorCanvas.gameObject.SetActive(shouldShow);

        if (shouldShow)
        {
            // Billboard: face the active camera
            Camera cam = Camera.main;
            if (cam != null)
                m_indicatorCanvas.transform.rotation = cam.transform.rotation;
        }
    }
}
