using Unity.Cinemachine;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        CinemachineBrain brain = Camera.main.GetComponent<CinemachineBrain>();
        ICinemachineCamera currentCam = brain.ActiveVirtualCamera;

        Debug.Log("The name of the camera"+currentCam.Name);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
