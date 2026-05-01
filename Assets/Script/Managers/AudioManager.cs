using PurrNet.Voice;
using UnityEngine;
using UnityEngine.InputSystem;

public class AudioManager : MonoBehaviour
{
    public void ProximityDefaultMode()
    {
        UnMutePlayers();
        //UnMuteAllPlayerLocally();
        UnMuteAllPlayerLocallyProximity();
    }

    public void MuteSinglePlayer()
    {
        foreach(GameObject obj in FindObjectsByType<GameObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                PurrVoicePlayer purrVoicePlayer = obj.GetComponent<PurrVoicePlayer>();
                if (purrVoicePlayer != null)
                {
                    if (!purrVoicePlayer.isOwner) continue;
                    if(!purrVoicePlayer.muted)
                    {
                        purrVoicePlayer.muted = true;
                    }
                }
            }
    }

    public void UnMutePlayers()
    {
        foreach(GameObject obj in FindObjectsByType<GameObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                PurrVoicePlayer purrVoicePlayer = obj.GetComponent<PurrVoicePlayer>();
                if (purrVoicePlayer != null)
                {
                    if(purrVoicePlayer.muted)
                    {
                        purrVoicePlayer.muted = false;
                    }
                }
            }
    }


    public void InitPushToTalk()
    {
        foreach(GameObject obj in FindObjectsByType<GameObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            PurrVoicePlayer purrVoicePlayer = obj.GetComponent<PurrVoicePlayer>();
            if (purrVoicePlayer != null)
            {
                if (!purrVoicePlayer.isOwner) continue;
                if(!purrVoicePlayer.muted)
                {
                    purrVoicePlayer.muted=true;
                }
            }
        }
    }
    public void PushToTalk(bool _push) 
    {
        foreach(GameObject obj in FindObjectsByType<GameObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if(obj.layer == LayerMask.NameToLayer("Child") || obj.layer == LayerMask.NameToLayer("Ghost"))
                {
                    PurrVoicePlayer purrVoicePlayer = obj.GetComponent<PurrVoicePlayer>();
                        if (purrVoicePlayer != null)
                        {
                            if (!purrVoicePlayer.isOwner) continue;
                            if(_push)
                            {
                                purrVoicePlayer.muted=false;
                            }
                            else
                            {
                                purrVoicePlayer.muted=true;
                            }
                        }   
                    }
                }
    }

    public void MuteAllPlayerLocally(bool _mute)
    {
        foreach(PlayerInput obj in FindObjectsByType<PlayerInput>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                AudioSource audioSource = obj.gameObject.GetComponent<AudioSource>();
                if (audioSource != null)
                {
                    audioSource.volume=_mute ? 0 : 1;
                }
            }
    }

    public void UnMuteAllPlayerLocallyProximity()
    {
        foreach(GameObject obj in FindObjectsByType<GameObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                PurrVoicePlayer purrVoicePlayer = obj.GetComponent<PurrVoicePlayer>();
                if (purrVoicePlayer != null)
                    {
                        if (purrVoicePlayer.isOwner) continue;
                        AudioSource audioSource = obj.gameObject.GetComponent<AudioSource>();
                        if (audioSource != null)
                        {
                            audioSource.volume=1;
                        }
                    }
                
            }
    }
}
