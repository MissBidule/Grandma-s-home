using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class TutoManager : MonoBehaviour
{

    void Start() //a fix
    {
        Debug.Log("ca se lance maintenant");
        foreach(GameObject obj in FindObjectsByType<GameObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
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

            }
    }

    // Update is called once per frame
    void Update()
    {
      
    }
}
