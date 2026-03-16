using UnityEngine;
using PurrNet;
using System.Collections;
using System.Collections.Generic;

public class PredictiveMovement : NetworkBehaviour
{
    private GameObject projectionObject;
    private readonly float frameRate = 1f / 30f;

    private List<ChildInputData> inputHistory = new List<ChildInputData>();
    private int tick = 0;
    private ChildInputController inputController;
    private ChildSimulateMovement simulateMovement;

    private ChildInputData currentInput = new();

    private void Start()
    {
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
        print("RESET");
        currentInput.wishDirection = Vector3.zero;
        currentInput.cameraYaw = 0f;
        currentInput.cameraPosition = Vector3.zero;
        currentInput.cameraForward = Vector3.zero;
        currentInput.jumpPressed = false;
        currentInput.switchPressed = false;
        currentInput.attackPressed = false;
        currentInput.sneakPressed = false;
    }

    // It was supposed to be revolutionary, it's just dogshit.
    public void NewInput(ChildInputData _data)
    {
        currentInput.wishDirection = _data.wishDirection;
        currentInput.cameraYaw = _data.cameraYaw;
        currentInput.cameraPosition = _data.cameraPosition;
        currentInput.cameraForward = _data.cameraForward;
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
            yield return new WaitForSeconds(frameRate);
            if (!isOwner) continue; // Je gère pas les autres (NE METTEZ PAS CA AU DESSUS DE YIELD CA VA EXPLOSER UNITY CEST UNE BOUCLE INFINI FAUT FORCEMENT ATTENDRE UNE FRAME PITIE) (en anglais)
            currentInput.tick = tick;

            // Si je suis l'hôte, je fais du mouvement normal, pas de prédiction
            if (isHost)
            {
                simulateMovement.SimulateMovement(currentInput);
                clearInputData();
                tick += 1;
                continue;
            }

            if (!isHost)
            {
                Debug.Log("Tick number: " + tick + " Movement: " + currentInput.wishDirection);
                // Je suis côté client
                simulateMovement.SimulateMovement(currentInput);
                inputHistory.Add(currentInput);
                clearInputData();
                tick += 1;
            }
        }
    }
}