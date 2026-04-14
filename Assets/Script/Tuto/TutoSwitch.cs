using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

/*
     * @brief  Contains class declaration for Switching between the Ghost Tuto and the Child Tutos
     */

public class SwitchTuto : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {

        if (other.CompareTag("Switch"))
        {
            TutoInstructions tutoInstructions = GetComponent<TutoInstructions>();
            tutoInstructions.HideTuto();
            
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
