using PurrNet;
using PurrNet.Voice;
using UnityEngine;
using UnityEngine.InputSystem;

public class AudioManager : MonoBehaviour
{
    public void MuteGhostByChild()
    {
        // mode par defaut les childs entendent que les Chils; et les Ghost entendent tout le monde (il faut que personne ne soit mute a la base)
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

    public void MuteAllPlayer() // ca devrait marcher en vrai non?
    {
        // mode pour deactive le proximity chat; personnes n'entend personne 
        foreach(GameObject obj in FindObjectsByType<GameObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
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

    public void PushToTalk(bool _push) 
    {
        // mode push to talk ; dans ce cas le mode par defaut est activé, (a shit genre ca pose pas de probleme puisque les ghost vont etre demute des childs, peut etre il faut mettre le mode par defaut dans un update alors!!) (ils doivent etre mute avant de lancer cette fonction)
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
