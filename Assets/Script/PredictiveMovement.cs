using UnityEngine;
using PurrNet;
using System.Collections;
using System.Collections.Generic;

public class PredictiveMovement : NetworkBehaviour
{
    private readonly float frameRate = 1f / 30f;

    private PredictiveMovement hostInstance;


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
        currentInput = new ChildInputData();
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
            
            if (isOwner) // PREDICTION
            {
                currentInput.tick = tick;
                
                simulateMovement.SimulateMovement(currentInput);
                if (!isHost) inputHistory.Add(currentInput);
                clearInputData();
            }

            // SERVER SIDE. ON APPLIQUE LES INPUTS DES AUTRES.
            if (isServer)
            {
                if (!isOwner) // Pas l'owner (= host) car il a déjà appliqué son input en prédiction
                {
                    simulateMovement.SimulateMovement(currentInput);
                }
                ClientReceiveCorrection(tick, transform.position, transform.rotation);
                clearInputData();
            }


            tick += 1;
        }
    }

    public void ServerReceiveInput(ChildInputData _data)
    {
        if (!isServer) return;
        if (isOwner) return;
        NewInput(_data);
    }

    [ObserversRpc(runLocally:false)]
    public void ClientReceiveCorrection(int tick, Vector3 position, Quaternion rotation)
    {
        if (isServer) return;
        if (!isOwner)
        {
            transform.position = position;
            transform.rotation = rotation;
            return;
        }
        else
        {
            // On supprime les inputs déjà appliqués par le serveur
            inputHistory.RemoveAll(input => input.tick <= tick);
            // On corrige la position du client
            Reconciliation(position, rotation);
        }
    }

    public void Reconciliation(Vector3 position, Quaternion rotation)
    {
        transform.position = position;
        transform.rotation = rotation;
        if (inputHistory.Count == 0) return;


        // On réapplique les inputs du client qui n'ont pas encore été appliqués par le serveur
        foreach (var input in inputHistory)
        {
            simulateMovement.SimulateMovement(input);
        }
    }
}