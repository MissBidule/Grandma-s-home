using UnityEngine;
using PurrNet;
using System.Collections;
using System.Collections.Generic;

public class PredictiveMovement : NetworkBehaviour
{
    private readonly float frameRate = 1f / 30f;

    private List<ChildInputData> inputHistory = new List<ChildInputData>();
    private int tick = 0;
    private ChildInputController inputController;
    private ChildSimulateMovement simulateMovement;

    private bool alreadySimulated = false;

    private ChildInputData currentInput = new();
    [SerializeField] private float errorThreshold;

    private void Start()
    {
        inputController = GetComponent<ChildInputController>();
        simulateMovement = GetComponent<ChildSimulateMovement>();
        clearInputData();

        StartCoroutine(PredictiveUpdate());
    }

    protected override void OnOwnerChanged(PurrNet.PlayerID? oldOwner, PurrNet.PlayerID? newOwner, bool asServer)
    {
        name = isOwner ? "Owner" : "Remote";
    }



    public int GetTick()
    {
        return tick;
    }

    private void clearInputData()
    {
        currentInput = new ChildInputData();
        currentInput.wishDirection = Vector3.zero;
        currentInput.cameraYaw = -1000f; // Valeur par défaut pour indiquer que la caméra n'a pas été mise à jour
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
        currentInput.position = transform.position;
    }

    private IEnumerator PredictiveUpdate()
    {
        while (true)
        {
            yield return new WaitForSeconds(frameRate);

            if (isOwner) // PREDICTION
            {
                currentInput.tick = tick;

                if (alreadySimulated) simulateMovement.SimulateMovement(currentInput);
                alreadySimulated = true;
                if (!isHost) inputHistory.Add(currentInput);
                clearInputData();
            }

            // SERVER SIDE. ON APPLIQUE LES INPUTS DES AUTRES.
            if (isServer)
            {
                if (!isOwner) // Pas l'owner (= host) car il a déjà appliqué son input en prédiction
                {
                    if (alreadySimulated) simulateMovement.SimulateMovement(currentInput);
                    alreadySimulated = true;
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
    public void ClientReceiveCorrection(int serverTick, Vector3 position, Quaternion rotation)
    {
        if (isServer) return;
        if (!isOwner)
        {
            transform.position = Vector3.Lerp(transform.position, position, 0.5f);
            transform.rotation = rotation;
            return;
        }
        else
        {
            // On supprime les inputs déjà appliqués par le serveur
            inputHistory.RemoveAll(input => input.tick < serverTick);
            // On corrige la position du client
            Reconciliation(position, rotation, serverTick);
        }
    }

    public void Reconciliation(Vector3 position, Quaternion _rotation, int serverTick)
    {
        var pos = position;
        var tpos = transform.position;

        
        // On réapplique les inputs du client qui n'ont pas encore été appliqués par le serveur
        transform.SetPositionAndRotation(pos, _rotation);

        for (var i = 0; i < inputHistory.Count; i++)
        {
            if (i == inputHistory.Count - 1 && alreadySimulated) break;
            simulateMovement.SimulateMovement(inputHistory[i]);
        }
        alreadySimulated = true;

        var fpos = transform.position;

        var error = Vector3.Distance(fpos, tpos);

        print(error);

        if (error > 1f)
        {
            print("rollback");
            transform.position = fpos; // donc ça rollback
        }
        else
        {
            // Si l'erreur est faible, on garde la position du client pour éviter les corrections trop brusques
            transform.position = Vector3.Lerp(tpos, fpos, Time.deltaTime * 10);
        }

    }
}