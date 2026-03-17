using PurrNet;
using PurrNet.Voice;
using UnityEngine;
using UnityEngine.InputSystem;

public class AudioManager : NetworkBehaviour
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
                        purrVoicePlayer.muted = true; // attend la j ai fait de la merde c pas sense se synchr faut faire 2 boolean oublie pas !!
                        }
                    }
                }
            }
    }

    public void PushToTalk(bool _push) //network du coup, mais ca va pas poser de blem avec MuteGhostByChild?? 
    {
        //faudrait mettre les deux options PushToTalk et Mode normal
        Debug.Log("il essaye de faire la fonction");
        foreach(GameObject obj in FindObjectsByType<GameObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if(obj.layer == LayerMask.NameToLayer("Child"))
                {
                    Debug.Log("il a trouve un objet child");
                    PlayerInput playerInput = obj.GetComponent<PlayerInput>();
                    PurrVoicePlayer purrVoicePlayer = obj.GetComponent<PurrVoicePlayer>();
                        //if(quesque je if ???)
                            if (purrVoicePlayer != null)
                            {
                                Debug.Log("il a un composent purrvoice");
                                if (!purrVoicePlayer.isOwner) continue;
                                Debug.Log("il est owner ce con?");
                                //if(!purrVoicePlayer.muted)
                                //{
                                if(_push)
                                {
                                    Debug.Log("il devrait retirer le mute");
                                    purrVoicePlayer.muted=false;
                                }
                                if(!_push)
                                {
                                    Debug.Log("il devrait mettre le mute");
                                    purrVoicePlayer.muted=true;
                                }
                                //}
                            }
                        
                }
            }
    }
}
