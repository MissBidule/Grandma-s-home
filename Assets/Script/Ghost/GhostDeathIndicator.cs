using UnityEngine;
using UnityEngine.UI;

/**
@brief       Displays a death indicator above a ghost's head when stopped
@details     Visible only to ghost players, renders through walls.
             Attach to the Ghost prefab alongside GhostController.
             Requires a child Canvas (World Space) named CanvasDeathIcon with an Image child.
*/
public class GhostDeathIndicator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Canvas m_indicatorCanvas;

    private GhostController m_ghostController;
    private bool m_isLocalPlayerGhost;
    private bool m_initialized;
    private Transform m_cameraTransform;

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
    @brief      Called by GhostController.ApplyStopToAll on all clients when this ghost is hit
    */
    public void OnGhostDied() { }

    private void LateUpdate()
    {
        if (!m_initialized)
        {
            // Find the ghost owned by the local player
            GhostController localGhost = null;
            foreach (var ghost in FindObjectsByType<GhostController>(FindObjectsSortMode.None))
            {
                if (ghost.isOwner)
                {
                    localGhost = ghost;
                    break;
                }
            }

            if (localGhost != null)
            {
                m_isLocalPlayerGhost = true;
                if (localGhost.m_playerCamera != null)
                    m_cameraTransform = localGhost.m_playerCamera.transform;
                m_initialized = true;
            }
            else
            {
                // If a child player is already owned locally, the local player is not a ghost
                foreach (var child in FindObjectsByType<ChildController>(FindObjectsSortMode.None))
                {
                    if (child.isOwner)
                    {
                        m_isLocalPlayerGhost = false;
                        m_initialized = true;
                        break;
                    }
                }
            }

            if (!m_initialized) return;
        }

        bool shouldShow = m_isLocalPlayerGhost && m_ghostController.m_isStopped;
        m_indicatorCanvas.gameObject.SetActive(shouldShow);

        if (shouldShow && m_cameraTransform != null)
        {
            // Billboard: face the local player's camera
            m_indicatorCanvas.transform.rotation = m_cameraTransform.rotation;
        }
    }
}
