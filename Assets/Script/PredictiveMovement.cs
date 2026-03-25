using UnityEngine;
using PurrNet;
using System.Collections;
using System.Collections.Generic;

public class PredictiveMovement : NetworkBehaviour
{
    public struct HistoricalState
    {
        public int tick;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 velocity;
    }

    private List<ChildInputData> inputHistory = new List<ChildInputData>();
    private List<HistoricalState> stateHistory = new List<HistoricalState>();
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
        var rb = GetComponent<Rigidbody>();
        if (isOwner) // PREDICTION
        {
            currentInput.tick = tick;

            if (alreadySimulated) simulateMovement.SimulateMovement(currentInput);
            alreadySimulated = true;
            if (!isHost) 
            {
                inputHistory.Add(currentInput);
                stateHistory.Add(new HistoricalState {
                    tick = tick,
                    position = transform.position,
                    rotation = transform.rotation,
                    velocity = rb.linearVelocity
                });
            }
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
                    ClientReceiveCorrection(lastProcessedClientTick, transform.position, transform.rotation, rb.linearVelocity);
                    lastSentCorrectionTick = lastProcessedClientTick;
                }
            }
            else
            {
                ClientReceiveCorrection(tick, transform.position, transform.rotation, rb.linearVelocity);
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
    public void ClientReceiveCorrection(int serverTick, Vector3 position, Quaternion rotation, Vector3 velocity)
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
            Reconciliation(position, rotation, velocity, serverTick);
        }
    }

    public void Reconciliation(Vector3 serverPos, Quaternion serverRot, Vector3 serverVel, int serverTick)
    {
        var rb = GetComponent<Rigidbody>();
        
        bool shouldRollback = true;
        int stateIndex = stateHistory.FindIndex(s => s.tick == serverTick);
        
        if (stateIndex != -1)
        {
            HistoricalState pastState = stateHistory[stateIndex];
            float distanceError = Vector3.Distance(pastState.position, serverPos);
            
            if (distanceError < errorThreshold)
            {
                shouldRollback = false; // La prédiction passée était exacte ! Pas de rollback !
            }
        }

        // On libère la mémoire de l'historique approuvé
        inputHistory.RemoveAll(input => input.tick <= serverTick);
        stateHistory.RemoveAll(s => s.tick <= serverTick);

        if (!shouldRollback) return; 

        // Sinon, la réalité diffère, on effectue un vrai rollback strict
        rb.rotation = serverRot;
        rb.position = serverPos; 
        rb.linearVelocity = serverVel; // Indispensable pour la courbe de saut !

        foreach (var input in inputHistory)
        {
            simulateMovement.SimulateMovement(input);
        }

        Physics.SyncTransforms(); // Appliquer immédiatement la physique des nouveaux transform modifiés
        alreadySimulated = true;
    }
}