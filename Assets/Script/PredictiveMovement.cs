using UnityEngine;
using PurrNet;
using System.Collections;
using System.Collections.Generic;

public struct PredictiveInputData
{
    public int tick;
    public Vector3 wishDirection;
    public float cameraYaw;
    public Vector3 cameraPosition;
    public Vector3 cameraForward;
    public bool jumpPressed;
    public bool switchPressed;
    public bool attackPressed;
    public bool sneakPressed;
    public bool dashPressed;
    public Vector3 position;
}

public class PredictiveMovement : NetworkBehaviour
{
    public struct HistoricalState
    {
        public int tick;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 velocity;
    }

    private List<PredictiveInputData> inputHistory = new List<PredictiveInputData>();
    private List<HistoricalState> stateHistory = new List<HistoricalState>();
    private int tick = 0;
    private int lastProcessedClientTick = 0;
    private int lastSentCorrectionTick = -1;
    private ISimulateMovement simulateMovement;

    private bool alreadySimulated = false;

    private PredictiveInputData currentInput = new();
    [SerializeField] private float errorThreshold;

    private void Start()
    {
        simulateMovement = GetComponent<ISimulateMovement>();
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
        currentInput = new PredictiveInputData();
        currentInput.wishDirection = Vector3.zero;
        currentInput.cameraYaw = -1000f; // Valeur par defaut pour indiquer que la camera n'a pas ete mise a jour
        currentInput.cameraPosition = Vector3.zero;
        currentInput.cameraForward = Vector3.zero;
        currentInput.jumpPressed = false;
        currentInput.switchPressed = false;
        currentInput.attackPressed = false;
        currentInput.sneakPressed = false;
        currentInput.dashPressed = false;
    }

    private void clearInputData()
    {
        // Seules les actions one-shot (boutons) doivent etre reset.
        // Ne PAS reset wishDirection, cameraYaw, sneakPressed ou jumpPressed, sinon le perso s'arrete entre deux FixedUpdates ou requetes reseau !
        currentInput.switchPressed = false;
        currentInput.attackPressed = false;
        currentInput.dashPressed = false;
    }

    // It was supposed to be revolutionary, it's just dogshit.
    public void NewInput(PredictiveInputData _data)
    {
        currentInput.wishDirection = _data.wishDirection;
        currentInput.cameraYaw = _data.cameraYaw;
        currentInput.cameraPosition = _data.cameraPosition;
        currentInput.cameraForward = _data.cameraForward;
        currentInput.jumpPressed = _data.jumpPressed;
        currentInput.switchPressed = currentInput.switchPressed | _data.switchPressed;
        currentInput.attackPressed = currentInput.attackPressed | _data.attackPressed;
        currentInput.sneakPressed = _data.sneakPressed;
        currentInput.dashPressed = currentInput.dashPressed | _data.dashPressed;
        currentInput.position = transform.position;
        // On enregistre le tick que le client nous a envoye
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
            if (!isOwner) // Pas l'owner (= host) car il a deja applique son input en prediction
            {
                if (alreadySimulated) simulateMovement.SimulateMovement(currentInput);
                alreadySimulated = true;
                
                // Le serveur ne renvoie une correction que s'il a traite un NOUVEL input du client
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

    public void ServerReceiveInput(PredictiveInputData _data)
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
                shouldRollback = false; // La prediction passee etait exacte ! Pas de rollback !
            }
        }

        // On libere la memoire de l'historique approuve
        inputHistory.RemoveAll(input => input.tick <= serverTick);
        stateHistory.RemoveAll(s => s.tick <= serverTick);

        if (!shouldRollback) return; 

        // Sinon, la realite differe, on effectue un vrai rollback strict
        rb.rotation = serverRot;
        rb.position = serverPos; 
        rb.linearVelocity = serverVel; // Indispensable pour la courbe de saut !

        foreach (var input in inputHistory)
        {
            simulateMovement.SimulateMovement(input);
        }

        Physics.SyncTransforms(); // Appliquer immediatement la physique des nouveaux transform modifies
        alreadySimulated = true;
    }
}