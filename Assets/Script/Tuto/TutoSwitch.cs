using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class SwitchTuto : MonoBehaviour
{

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {

        if (other.CompareTag("Switch"))
        {
            GameObject otherPlayer = GetOtherPlayer();

            PlayerInput playerInput = GetComponent<PlayerInput>();
            if (playerInput != null)
            {
                playerInput.enabled = false;
            }
            CinemachineCamera cinemachineCamera = GetComponentInChildren<CinemachineCamera>();
            if(cinemachineCamera != null)
            {
                cinemachineCamera.enabled = false;
            }

            PlayerInput otherplayerInput = otherPlayer.GetComponent<PlayerInput>();
            if (otherplayerInput != null)
            {
                otherplayerInput.enabled = true;
            }
            CinemachineCamera othercinemachineCamera = otherPlayer.GetComponentInChildren<CinemachineCamera>();
            if(othercinemachineCamera != null)
            {
                othercinemachineCamera.enabled = true;
            }
        }
    }

    private GameObject GetOtherPlayer()
    {
        if (CompareTag("Child"))
            return GameObject.FindGameObjectWithTag("Ghost");

        if (CompareTag("Ghost"))
            return GameObject.FindGameObjectWithTag("Child");

        return null;
    }
}
