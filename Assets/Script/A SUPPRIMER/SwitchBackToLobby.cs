using PurrNet;
using PurrNet.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SwitchBackToLobby : MonoBehaviour
{
    [PurrScene, SerializeField] private string nextScene;

    void Start()
    {
        _hasAlreadySwitched = false; // Reset flag on start to allow scene switching in new lobby sessions
    }

    private static bool _hasAlreadySwitched = false;
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Child") || collision.gameObject.layer == LayerMask.NameToLayer("Ghost"))
        {
            Debug.Log("Collision detected with player, switching back to lobby...");
            SwitchScene();
        }
    }

    public void SwitchScene()
        {
            // Prevent duplicate scene switches
            if (_hasAlreadySwitched)
            {
                PurrLogger.LogWarning("SwitchScene already called - ignoring duplicate", this);
                return;
            }
            
            _hasAlreadySwitched = true;
            
            if (string.IsNullOrEmpty(nextScene))
            {
                PurrLogger.LogError("Next scene name is not set!", this);
                return;
            }

            PurrLogger.Log($"Switching to scene: {nextScene}", this);
            
            // Load game scene - ConnectionStarter in new scene will handle network initialization
            SceneManager.LoadSceneAsync(nextScene);
        }
}
