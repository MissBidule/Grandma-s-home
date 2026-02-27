using PurrLobby;
using System;
using System.Collections;
using PurrNet;
using PurrNet.Logging;
using PurrNet.Transports;
using UnityEngine;
using TMPro;
using UnityEngine.Serialization;
using PurrNet.StateMachine;

public class CustomConnectedText : MonoBehaviour
{
    [SerializeField] private NetworkManager m_networkManager;
    [SerializeField] private TMP_Text m_connectedText;
    [SerializeField] private TMP_Text m_waitingText;
    [SerializeField] private TMP_Text m_playerText;

    private String m_messageContent = "Not Connected\nWaiting for Other Player\nCurrently X out of X";
    private LobbyDataHolder m_lobbyDataHolder;
    
    private Coroutine m_typewriterEffect0;
    private Coroutine m_typewriterEffect1;
    private Coroutine m_typewriterEffect2;
    
    private void Awake()
    {
        // Connection Event
        m_networkManager.onClientConnectionState += OnConnectionState;
        
        // Number Of players
        m_lobbyDataHolder = FindFirstObjectByType<LobbyDataHolder>();
        if(!m_lobbyDataHolder) {
            PurrLogger.LogError($"Failed to get {nameof(LobbyDataHolder)} component.", this);
            return;
        }

        string[] messages = m_messageContent.Split("\n");
        messages[2] = "Currently 0 out of " + m_lobbyDataHolder.GetNumber_of_player_in_lobby();
        m_playerText.text = messages[2];

        m_networkManager.onPlayerJoined += OnPlayerJoined;
    }

    private void OnDestroy()
    {
        m_networkManager.onClientConnectionState -= OnConnectionState;
        m_networkManager.onPlayerJoined -= OnPlayerJoined;
    }

    /*
     * @brief Changing message for connection state.
     * @input obj (state of connection)
     */
    private void OnConnectionState(ConnectionState _obj)
    {
        if (!gameObject.activeInHierarchy) 
            return;
        string[] messages = m_messageContent.Split("\n");
        
        if (_obj == ConnectionState.Connected)
            messages[0] = "Connected";
        else if (_obj == ConnectionState.Disconnected)
            messages[0] = "Not connected";

        m_messageContent = messages[0] + "\n" + messages[1] + "\n" + messages[2];

        if (m_typewriterEffect0 != null)
        {
            StopCoroutine(m_typewriterEffect0);
        }
        m_typewriterEffect0 = StartCoroutine(TypewriterEffect(0));
    }

    private void OnPlayerJoined(PlayerID _player, bool _isReconnect, bool _asServer)
    {
        if (!gameObject.activeInHierarchy) 
            return;
        OnNumberOfPlayersChanged(m_networkManager.playerCount, m_lobbyDataHolder.GetNumber_of_player_in_lobby());
    }

    private void OnNumberOfPlayersChanged(int _playerNumber, int _maxPlayerNumber)
    {
        string[] messages = m_messageContent.Split("\n");
        messages[2] = $"Currently {_playerNumber} out of {_maxPlayerNumber}";
        m_messageContent = messages[0] + "\n" + messages[1] + "\n" + messages[2];
        
        if (m_typewriterEffect2 != null)
        {
            StopCoroutine(m_typewriterEffect2);
        }
        m_typewriterEffect2 = StartCoroutine(TypewriterEffect(2));

        if (_playerNumber == _maxPlayerNumber)
        {
            StateMachine stateMachine = FindFirstObjectByType<StateMachine>();
            if (!stateMachine)
            {
                PurrLogger.LogError($"Failed to get {nameof(StateMachine)} component.", this);
                return;
            }
            else ((PlayerSpawningState)stateMachine.states[1]).StartMachine();
        }
    }

    private WaitForSeconds m_wait = new(0.005f);
    
    private IEnumerator TypewriterEffect(int _textBox)
    {
        string message = m_messageContent.Split("\n")[_textBox];
        switch (_textBox)
        {
            case 0:
                while (m_connectedText.text.Length > 0)
                {
                    m_connectedText.text = m_connectedText.text.Substring(0, m_connectedText.text.Length - 1);
                    yield return m_wait;
                }

                foreach (char c in message)
                {
                    m_connectedText.text += c;
                    yield return m_wait;
                }
                break;
            case 1:
                while (m_waitingText.text.Length > 0)
                {
                    m_waitingText.text = m_waitingText.text.Substring(0, m_waitingText.text.Length - 1);
                    yield return m_wait;
                }

                foreach (char c in message)
                {
                    m_waitingText.text += c;
                    yield return m_wait;
                }
                break;
            case 2:
                while (m_playerText.text.Length > 10)
                {
                    m_playerText.text = m_playerText.text.Substring(0, m_playerText.text.Length - 1);
                    yield return m_wait;
                }

                int skipCounter = 0;
                foreach (char c in message)
                {
                    skipCounter++;
                    if (skipCounter < 10)
                        continue;
                    m_playerText.text += c;
                    
                    yield return m_wait;
                }
                break;
            
        }
    }
}