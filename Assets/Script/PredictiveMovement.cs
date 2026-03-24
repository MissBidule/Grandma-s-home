using UnityEngine;
using PurrNet;
using System.Collections;
using System.Collections.Generic;

public class PredictiveMovement : NetworkBehaviour
{

    private List<ChildInputData> inputHistory = new List<ChildInputData>();
    private int tick = 0;
    private int lastProcessedClientTick = 0;
    private int lastSentCorrectionTick = -1;
    private ChildInputController inputController;
    private ChildSimulateMovement simulateMovement;

    private bool alreadySimulated = false;

    private ChildInputData currentInput = new();
    [SerializeField] private float errorThreshold;

    private void Start()
    {
        inputController = GetComponent<ChildInputController>();
        simulateMovement = GetComponent<ChildSimulateMovement>();
        InitInputData();

        //StartCoroutine(PredictiveUpdate());
    }

    private void FixedUpdate()
    {
        Tick();
    }

    protected override void OnOwnerChanged(PurrNet.PlayerID? oldOwner, PurrNet.PlayerID? newOwner, bool asServer)
    {
        name = isOwner ? "Owner" : "Remote";
    }


    public int GetTick()
    {
        return tick;
    }

    private void InitInputData()
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

    private void clearInputData()
    {
        // Seules les actions one-shot (boutons) doivent être reset. 
        // Ne PAS reset wishDirection, cameraYaw ou sneakPressed, sinon le perso s'arrête entre deux FixedUpdates ou requêtes réseau !
        currentInput.jumpPressed = false;
        currentInput.switchPressed = false;
        currentInput.attackPressed = false;
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
        // On enregistre le tick que le client nous a envoyé
        lastProcessedClientTick = _data.tick; 
    }

    private void Tick()
    {
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
                
                // Le serveur ne renvoie une correction que s'il a traité un NOUVEL input du client
                if (lastProcessedClientTick != lastSentCorrectionTick)
                {
                    ClientReceiveCorrection(lastProcessedClientTick, transform.position, transform.rotation);
                    lastSentCorrectionTick = lastProcessedClientTick;
                }
            }
            else
            {
                ClientReceiveCorrection(tick, transform.position, transform.rotation);
            }
            clearInputData();
        }


        tick += 1;
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
            // On corrige la position du client
            Reconciliation(position, rotation, serverTick);
        }
    }

    public void Reconciliation(Vector3 serverPos, Quaternion serverRot, int serverTick)
    {
        var rb = GetComponent<Rigidbody>();
        
        // 1. Oter tous les inputs jusqu'au tick serveur INCLUS
        inputHistory.RemoveAll(input => input.tick <= serverTick);

        // 2. Vérifier si l'erreur est suffisamment grande pour justifier un rollback
        float distanceError = Vector3.Distance(rb.position, serverPos);
        if (distanceError < errorThreshold) // ex: errorThreshold = 0.1f
        {
            return; // Prédiction "assez bonne", on ne corrige pas pour éviter les saccades
        }

        // Sinon, on remet le joueur sur la position stricte du serveur
        rb.rotation = serverRot;
        rb.position = serverPos; 
        // Au lieu de Lerp ou MoveTowards ici, en vrai CSP on snap, et on rejoue l'history
        // Si vous voulez du lissage visuel, mettez un objet visuel enfant lissé, mais le Rigidbody DOIT snaper.

        // 3. Réappliquer la physique/les inputs ratés (Re-simulation)
        // ATTENTION: ChildSimulateMovement doit modifier rb.position et PAS MovePosition pendant cette boucle, 
        // ou alors vous devez appeler Physics.Simulate() si c'est indispensable.
        foreach (var input in inputHistory)
        {
            simulateMovement.SimulateMovement(input);
        }

        Physics.SyncTransforms(); // Appliquer immédiatement la physique des nouveaux transform modifiés
        alreadySimulated = true;
    }
}