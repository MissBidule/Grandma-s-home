using UnityEngine;
using PurrNet;
using System.Collections;
using System.Collections.Generic;

/*
 * @brief Struct to hold shared input states (Movement, camera, actions) between client and server across multiple network ticks.
 * @details Instances of this are used to feed ISimulateMovement execution loops and replay missed inputs during reconciliations.
 */
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

/*
 * @brief Core engine handling Client-Side Prediction (CSP) loops and rollbacks.
 * @details Keeps a buffer of inputs and states. Re-simulates mechanics via the ISimulateMovement interface whenever real Server data proves a past local prediction was incorrect.
 */
public class PredictiveMovement : NetworkBehaviour
{
    /*
     * @brief Struct to hold physical transform data natively at a recorded past tick in order to check prediction threshold mismatches securely.
     */
    public struct HistoricalState
    {
        public int tick;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 velocity;
    }

    private List<PredictiveInputData> m_inputHistory = new List<PredictiveInputData>();
    private List<HistoricalState> m_stateHistory = new List<HistoricalState>();
    private int m_tick = 0;
    private int m_lastProcessedClientTick = 0;
    private int m_lastSentCorrectionTick = -1;
    private ISimulateMovement m_simulateMovement;

    private bool m_alreadySimulated = false;

    private PredictiveInputData m_currentInput = new();
    private float m_errorThreshold = 0.75f; // Fuck you

    private void Start()
    {
        m_simulateMovement = GetComponent<ISimulateMovement>();
        InitInputData();

        //StartCoroutine(PredictiveUpdate());
    }

    private void FixedUpdate()
    {
        Tick();
    }

    protected override void OnOwnerChanged(PurrNet.PlayerID? _oldOwner, PurrNet.PlayerID? _newOwner, bool _asServer)
    {
        name = isOwner ? "Owner" : "Remote";
    }


    public int GetTick()
    {
        return m_tick;
    }

    /*
     * @brief Initializes the input data structure with default values
     * @return void
     */
    private void InitInputData()
    {
        m_currentInput = new PredictiveInputData();
        m_currentInput.wishDirection = Vector3.zero;
        m_currentInput.cameraYaw = -1000f; // Default value indicating camera has not been updated
        m_currentInput.cameraPosition = Vector3.zero;
        m_currentInput.cameraForward = Vector3.zero;
        m_currentInput.jumpPressed = false;
        m_currentInput.switchPressed = false;
        m_currentInput.attackPressed = false;
        m_currentInput.sneakPressed = false;
        m_currentInput.dashPressed = false;
    }

    /*
     * @brief Clears the one-shot actions from current input
     * @return void
     */
    private void clearInputData()
    {
        // Only one-shot actions (buttons) should be reset. 
        // DO NOT reset wishDirection, cameraYaw or sneakPressed, otherwise the character stops between two FixedUpdates or network requests!
        m_currentInput.jumpPressed = false;
        m_currentInput.switchPressed = false;
        m_currentInput.attackPressed = false;
        m_currentInput.dashPressed = false;
    }

    /*
     * @brief Registers new input data from the client
     * @param _data The predictive input data received
     * @return void
     */
    public void NewInput(PredictiveInputData _data)
    {
        m_currentInput.wishDirection = _data.wishDirection;
        m_currentInput.cameraYaw = _data.cameraYaw;
        m_currentInput.cameraPosition = _data.cameraPosition;
        m_currentInput.cameraForward = _data.cameraForward;
        m_currentInput.jumpPressed = m_currentInput.jumpPressed | _data.jumpPressed;
        m_currentInput.switchPressed = m_currentInput.switchPressed | _data.switchPressed;
        m_currentInput.attackPressed = m_currentInput.attackPressed | _data.attackPressed;
        m_currentInput.sneakPressed = _data.sneakPressed;
        m_currentInput.dashPressed = m_currentInput.dashPressed | _data.dashPressed;
        m_currentInput.position = transform.position;
        // Save the tick sent by the client
        m_lastProcessedClientTick = _data.tick;
    }

    /*
     * @brief Physics tick handling prediction and server reconciliation
     * @return void
     */
    private void Tick()
    {
        var rb = GetComponent<Rigidbody>();
        if (isOwner) // PREDICTION
        {
            m_currentInput.tick = m_tick;

            if (m_alreadySimulated) m_simulateMovement.SimulateMovement(m_currentInput);
            m_alreadySimulated = true;
            if (!isHost)
            {
                m_inputHistory.Add(m_currentInput);
                m_stateHistory.Add(new HistoricalState
                {
                    tick = m_tick,
                    position = transform.position,
                    rotation = transform.rotation,
                    velocity = rb.linearVelocity
                });
            }
            clearInputData();
        }

        // SERVER SIDE. APPLY OTHER PLAYERS' INPUTS.
        if (isServer)
        {
            if (!isOwner) // Not the owner (= host) because they already applied their input in prediction
            {
                if (m_alreadySimulated) m_simulateMovement.SimulateMovement(m_currentInput);
                m_alreadySimulated = true;

                // The server only sends a correction if it has processed a NEW input from the client
                if (m_lastProcessedClientTick != m_lastSentCorrectionTick)
                {
                    ClientReceiveCorrection(m_lastProcessedClientTick, transform.position, transform.rotation, rb.linearVelocity);
                    m_lastSentCorrectionTick = m_lastProcessedClientTick;
                }
            }
            else
            {
                ClientReceiveCorrection(m_tick, transform.position, transform.rotation, rb.linearVelocity);
            }
            clearInputData();
        }


        m_tick += 1;
    }

    /*
     * @brief Server receives the input data from the client
     * @param _data The predictive input data
     * @return void
     */
    public void ServerReceiveInput(PredictiveInputData _data)
    {
        if (!isServer) return;
        if (isOwner) return;
        NewInput(_data);
    }

    /*
     * @brief Client receives the corrected position and velocity from the server
     * @param _serverTick The server tick corresponding to the state
     * @param _position The server position
     * @param _rotation The server rotation
     * @param _velocity The server velocity
     * @return void
     */
    [ObserversRpc(runLocally: false)]
    public void ClientReceiveCorrection(int _serverTick, Vector3 _position, Quaternion _rotation, Vector3 _velocity)
    {
        if (isServer) return;
        if (!isOwner)
        {
            transform.position = Vector3.Lerp(transform.position, _position, 0.5f);
            transform.rotation = _rotation;
            return;
        }
        else
        {
            // Correct the client's position
            Reconciliation(_position, _rotation, _velocity, _serverTick);
        }
    }

    /*
     * @brief Reconciles the client's state with the server's authoritative state
     * @param _serverPos The true server position
     * @param _serverRot The true server rotation
     * @param _serverVel The true server velocity
     * @param _serverTick The tick at which the server recorded this state
     * @return void
     */
    public void Reconciliation(Vector3 _serverPos, Quaternion _serverRot, Vector3 _serverVel, int _serverTick)
    {
        var rb = GetComponent<Rigidbody>();

        bool shouldRollback = true;
        int stateIndex = m_stateHistory.FindIndex(s => s.tick == _serverTick);

        if (stateIndex != -1)
        {
            HistoricalState pastState = m_stateHistory[stateIndex];

            // Compare only horizontal velocity to ignore gravity differences
            Vector3 predictedHorizontalVel = new Vector3(pastState.velocity.x, 0f, pastState.velocity.z);
            Vector3 serverHorizontalVel = new Vector3(_serverVel.x, 0f, _serverVel.z);

            float distanceError = Vector3.Distance(pastState.position, _serverPos);

            if (distanceError < m_errorThreshold)
            {
                shouldRollback = false; // The past prediction was acceptable

                // Apply soft correction to prevent drift accumulation
                // Smoothly move client state towards server state
                rb.position = Vector3.Lerp(rb.position, _serverPos, 0.1f);
                rb.rotation = Quaternion.Lerp(rb.rotation, _serverRot, 0.1f);
                // Only correct horizontal velocity to avoid gravity interference
                Vector3 currentVel = rb.linearVelocity;
                Vector3 correctedVel = Vector3.Lerp(new Vector3(currentVel.x, currentVel.y, currentVel.z),
                                                     new Vector3(serverHorizontalVel.x, currentVel.y, serverHorizontalVel.z),
                                                     0.1f);
                rb.linearVelocity = correctedVel;
            }
        }

        // Free the memory of the approved history
        m_inputHistory.RemoveAll(input => input.tick <= _serverTick);
        m_stateHistory.RemoveAll(s => s.tick <= _serverTick);

        if (!shouldRollback) return;

        // Otherwise, reality differs, perform a true strict rollback
        rb.rotation = _serverRot;
        rb.position = _serverPos;
        rb.linearVelocity = _serverVel; // Essential for the jump curve!

        foreach (var input in m_inputHistory)
        {
            m_simulateMovement.SimulateMovement(input);
        }

        Physics.SyncTransforms(); // Immediately apply the physics of the newly modified transforms
        m_alreadySimulated = true;
    }
}