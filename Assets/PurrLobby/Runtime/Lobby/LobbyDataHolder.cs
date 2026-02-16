using System;
using UnityEngine;

namespace PurrLobby
{
    public class LobbyDataHolder : MonoBehaviour
    {
        [SerializeField] private Lobby serializedLobby;
        public Lobby CurrentLobby { get; private set; }

        private int number_of_player_in_loby =-1;

        public void SetCurrentLobby(Lobby newLobby)
        {
            CurrentLobby = newLobby;
            serializedLobby = newLobby;
        }

        public void setNumber_of_player_in_loby(int number_of_player_in_loby)
        {
            this.number_of_player_in_loby = number_of_player_in_loby;
        }

        public int GetNumber_of_player_in_loby()
        {
            return number_of_player_in_loby;
        }
        
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
    }
}
