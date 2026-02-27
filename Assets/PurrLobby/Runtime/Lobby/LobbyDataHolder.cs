using System;
using UnityEngine;

namespace PurrLobby
{
    public class LobbyDataHolder : MonoBehaviour
    {
        [SerializeField] private Lobby serializedLobby;
        public Lobby CurrentLobby { get; private set; }

        private int number_of_player_in_lobby =-1;

        public void SetCurrentLobby(Lobby _newLobby)
        {
            CurrentLobby = _newLobby;
            serializedLobby = _newLobby;
        }

        public void SetMaxPlayer(int _max_players)
        {
            serializedLobby.MaxPlayers = _max_players;
        }

        public void setNumber_of_player_in_lobby(int number_of_player_in_lobby)
        {
            this.number_of_player_in_lobby = number_of_player_in_lobby;
        }

        public int GetNumber_of_player_in_lobby()
        {
            return number_of_player_in_lobby;
        }
        
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
    }
}
