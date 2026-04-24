using System;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

/*
     * @brief  Contains class declaration for Switching between the Ghost Tuto and the Child Tutos
     */

public class SwitchTuto : MonoBehaviour
{
    private Transform m_GhostTransform;
    private Transform m_ChildTransform;
    [SerializeField] private bool m_IsGhost=false;

    void Awake()
    {
        foreach(GameObject obj in FindObjectsByType<GameObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if(obj.name == "SpawnPoint1")
            {
               m_ChildTransform = obj.transform;
            }  
            if(obj.name == "SpawnPoint2")
            {
                m_GhostTransform = obj.transform;
            }    
        }
    }
    private void OnTriggerEnter(Collider other)
    {

        if (other.CompareTag("Switch"))
        {
            //changer la position du joueur ici
            if (m_IsGhost)
            {
                transform.position = m_GhostTransform.position;
            }
            else
            {
                transform.position = m_ChildTransform.position;
            }
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
            AudioListener audioListener = GetComponentInChildren<AudioListener>();
            if(audioListener != null)
            {
                audioListener.enabled = false;
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
            AudioListener otheraudioListener = otherPlayer.GetComponentInChildren<AudioListener>();
            if(otheraudioListener != null)
            {
                otheraudioListener.enabled = true;
            }

            foreach(GameObject obj in FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if(obj.layer == LayerMask.NameToLayer("UI"))
                {
                    if((obj.name == "GhostUIHolder(Clone)")||(obj.name == "ChildUIHolder(Clone)"))
                    {
                        if(obj.activeInHierarchy)
                        {
                            obj.SetActive(false);
                        }
                        else
                        {
                            obj.SetActive(true);
                        }
                    }  
                }
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
