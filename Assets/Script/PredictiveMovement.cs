using UnityEngine;
using PurrNet;
using System.Collections;
using System.Collections.Generic;

public class PredictiveMovement : NetworkBehaviour
{
    private GameObject projectionObject;
    private readonly float frameRate = 1f / 60f;

    private List<ChildInputData> inputHistory = new List<ChildInputData>();
    private int tick = 0;
    private ChildInputController inputController;
    private ChildSimulateMovement simulateMovement;

    private ChildInputData currentInput = new();

    private NetworkManager networkManager;

    private void Start()
    {
        networkManager = FindAnyObjectByType<NetworkManager>();
        inputController = GetComponent<ChildInputController>();
        simulateMovement = GetComponent<ChildSimulateMovement>();
        clearInputData();


        StartCoroutine(PredictiveUpdate());
    }
    public int GetTick()
    {
        return tick;
    }

    private void clearInputData()
    {
        currentInput.wishDirection = Vector3.zero;
        currentInput.cameraYaw = 0f;
        currentInput.cameraPosition = Vector3.zero;
        currentInput.cameraForward = Vector3.zero;
        currentInput.jumpPressed = false;
        currentInput.switchPressed = false;
        currentInput.attackPressed = false;
        currentInput.sneakPressed = false;
    }

    public void NewInput(ChildInputData _data)
    {
        currentInput.wishDirection += _data.wishDirection;
        currentInput.cameraYaw += _data.cameraYaw;
        currentInput.cameraPosition += _data.cameraPosition;
        currentInput.cameraForward += _data.cameraForward;
        currentInput.jumpPressed = currentInput.jumpPressed | _data.jumpPressed;
        currentInput.switchPressed = currentInput.switchPressed | _data.switchPressed;
        currentInput.attackPressed = currentInput.attackPressed | _data.attackPressed;
        currentInput.sneakPressed = currentInput.sneakPressed | _data.sneakPressed;
    }

    private IEnumerator PredictiveUpdate()
    {
        while (true)
        {
            name = isOwner ? "Owner":"Remote";
            if (!isOwner) continue; // Je gère pas les autres
            yield return new WaitForSeconds(frameRate);
            currentInput.tick = tick;

            // Si je suis l'hôte, je fais du mouvement normal, pas de prédiction
            if (isHost)
            {
                simulateMovement.SimulateMovement(currentInput);
                tick += 1;
                //Debug.Log("Tick number: " + tick + " Movement: " + currentInput.wishDirection);
                Debug.Log("Je suis " + name + " mais je me considère Host, et c'est ok.");
                
                continue;
            }

            if (!isHost)
            {
                // Je suis côté client
                Debug.Log("Je suis " + name + " mais je me considère Client, et c'est ok.");
                simulateMovement.SimulateMovement(currentInput);
                inputHistory.Add(currentInput);
                clearInputData();
            }
            tick += 1;
            //Debug.Log("Tick number: " + tick + " Movement: " + currentInput.wishDirection);
        }
    }
}