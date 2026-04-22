using UnityEngine;
using Unity.Cinemachine;

//hides the target Canvas (via Canvas.enabled) while any watched GameObject is active
//or while any watched Cinemachine camera is prioritized as live
public class HideWhileAnyActive : MonoBehaviour
{
    [Tooltip("Canvas to hide when any watch target is active. If null, uses the Canvas on this GameObject.")]
    public Canvas targetCanvas;

    [Tooltip("GameObjects to watch. If any is active in hierarchy, targetCanvas is hidden.")]
    public GameObject[] watchTargets;

    [Tooltip("Cinemachine cameras to watch. If any has priority above livePriorityThreshold, targetCanvas is hidden.")]
    public CinemachineVirtualCameraBase[] watchCameras;

    [Tooltip("Priority value above which a watched camera is considered live.")]
    public int livePriorityThreshold = 10;

    private void Awake()
    {
        if (targetCanvas == null) targetCanvas = GetComponent<Canvas>();
    }

    private void LateUpdate()
    {
        if (targetCanvas == null) return;
        bool anyActive = false;
        if (watchTargets != null)
        {
            for (int i = 0; i < watchTargets.Length; i++)
            {
                if (watchTargets[i] != null && watchTargets[i].activeInHierarchy)
                {
                    anyActive = true;
                    break;
                }
            }
        }
        if (!anyActive && watchCameras != null)
        {
            for (int i = 0; i < watchCameras.Length; i++)
            {
                if (watchCameras[i] != null && watchCameras[i].Priority > livePriorityThreshold)
                {
                    anyActive = true;
                    break;
                }
            }
        }
        bool shouldBeEnabled = !anyActive;
        if (targetCanvas.enabled != shouldBeEnabled)
            targetCanvas.enabled = shouldBeEnabled;
    }
}
