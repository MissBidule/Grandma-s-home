using UnityEngine;

namespace PurrLobby
{
    /**
    @brief       Parameters needed at the start of the game
    @details     Will play once when starting the execution
    */
    public class AppSettings : MonoBehaviour
    {
        void Start()
        {
            Application.runInBackground = true;
        }
    }
}
