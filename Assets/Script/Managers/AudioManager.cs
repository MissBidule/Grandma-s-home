using PurrNet.Voice;
using UnityEngine;
using UnityEngine.InputSystem;

public class AudioManager : MonoBehaviour
{
    public void ProximityDefaultMode()
    {
        UnMutePlayers();
    }

    public void MuteSinglePlayer()
    {
        foreach(PlayerControllerCore obj in FindObjectsByType<PlayerControllerCore>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            PurrVoicePlayer purrVoicePlayer = obj.gameObject.GetComponent<PurrVoicePlayer>();
            if (purrVoicePlayer != null)
            {
                if (!purrVoicePlayer.isOwner) continue;
                purrVoicePlayer.muted=true;
                return;
            }
        }
    }

    public void UnMutePlayers()
    {
        foreach(PlayerControllerCore obj in FindObjectsByType<PlayerControllerCore>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            PurrVoicePlayer purrVoicePlayer = obj.gameObject.GetComponent<PurrVoicePlayer>();
            if (purrVoicePlayer != null)
            {
                purrVoicePlayer.muted = false;
            }
        }
    }


    public void InitPushToTalk()
    {
        MuteSinglePlayer();
    }

    public void PushToTalk(bool _push) 
    {
        foreach(PlayerControllerCore obj in FindObjectsByType<PlayerControllerCore>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!obj.isOwner) continue;
            PurrVoicePlayer purrVoicePlayer = obj.gameObject.GetComponent<PurrVoicePlayer>();
            if (purrVoicePlayer != null)
            {
                if(_push)
                {
                    purrVoicePlayer.muted=false;
                }
                else
                {
                    purrVoicePlayer.muted=true;
                }
            } 
            return;  
        }
    }

    public void MuteAllPlayerLocally(bool _mute)
    {
        foreach(PlayerControllerCore obj in FindObjectsByType<PlayerControllerCore>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (obj.isOwner) continue;
            AudioSource audioSource = obj.gameObject.GetComponent<AudioSource>();
            if (audioSource != null)
            {
                audioSource.volume = _mute ? 0 : 1;
            }
        }
    }
}
