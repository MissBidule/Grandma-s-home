using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using PurrLobby;

public class TutoInitialisation : MonoBehaviour
{
    private bool m_tutoOn;
    void Start()
    {
        foreach(SceneSwitcher sceneSwitcher in FindObjectsByType<SceneSwitcher>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            m_tutoOn=sceneSwitcher._isTuto;
        }
        foreach(GameObject obj in FindObjectsByType<GameObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if(m_tutoOn)
            {
                if(obj.layer == LayerMask.NameToLayer("Ghost"))
                {
                    PlayerInput ghostplayerInput = obj.GetComponent<PlayerInput>();
                    if (ghostplayerInput != null)
                    {
                        ghostplayerInput.enabled = false;
                    }

                    CinemachineCamera ghostcinemachineCamera = obj.GetComponentInChildren<CinemachineCamera>();
                    if(ghostcinemachineCamera != null)
                    {
                        ghostcinemachineCamera.enabled = false;
                    }
                }
                if(obj.layer == LayerMask.NameToLayer("UI"))
                {
                    if (obj.GetComponent<TutoInitialisation>() != null)
                    {
                        obj.SetActive(false);
                    }
                }
            }
        }
    }
}
