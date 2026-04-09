using UnityEngine;

namespace PurrLobby
{
    public struct LobbyUser
    {
        public string Id;
        public string DisplayName;
        public bool IsReady;
        public bool IsGhost;
        public bool IsInGame;
        public int Skin;
    }
}