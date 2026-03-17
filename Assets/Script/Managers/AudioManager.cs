using PurrNet;
using PurrNet.Voice;
using UnityEngine;
using UnityEngine.InputSystem;

public class AudioManager : MonoBehaviour
{
    public void MuteGhostByChild()
    {
            foreach(GameObject obj in FindObjectsByType<GameObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if(obj.layer == LayerMask.NameToLayer("Ghost"))
                {
                    PurrVoicePlayer purrVoicePlayer = obj.GetComponent<PurrVoicePlayer>();
                    if (purrVoicePlayer != null)
                    {
                        if(!purrVoicePlayer.muted)
                        {
                        purrVoicePlayer.muted = true;
                        }
                    }
                }
            }
    }

    public void PushToTalk(bool _push) 
    {
        foreach(GameObject obj in FindObjectsByType<GameObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if(obj.layer == LayerMask.NameToLayer("Child"))
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
}
