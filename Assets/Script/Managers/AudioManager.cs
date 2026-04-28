using PurrNet.Voice;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    // Faut aussi gérer au début toujours mettre le bon mode (faut que le mode qui est écrit au début de partie s'applique vraiment)
    public void MuteGhostByChild()
    {
        Debug.Log("MUTE GHOST BY CHILD CALL");
            foreach(GameObject obj in FindObjectsByType<GameObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if(obj.layer == LayerMask.NameToLayer("Ghost"))
                {
                    PurrVoicePlayer purrVoicePlayer = obj.GetComponent<PurrVoicePlayer>();
                    if (purrVoicePlayer != null)
                    {
                        if(!purrVoicePlayer.muted)
                        {
                        purrVoicePlayer.muted=true;
                        }
                    }
                }
                if(obj.layer == LayerMask.NameToLayer("Child"))
                {
                    PurrVoicePlayer purrVoicePlayer = obj.GetComponent<PurrVoicePlayer>();
                    if (purrVoicePlayer != null)
                    {
                        if(purrVoicePlayer.muted)
                        {
                        purrVoicePlayer.muted=false;
                        }
                    }
                }
            }
    }

    public void MuteSinglePlayer() //A verifier
    {
        Debug.Log("MUTE SINGLE PLAYER CALL");
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

    public void UnMuteSinglePlayer() //A verifier
    {
        Debug.Log("UN MUTE SINGLE PLAYER CALL");
        foreach(GameObject obj in FindObjectsByType<GameObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                PurrVoicePlayer purrVoicePlayer = obj.GetComponent<PurrVoicePlayer>();
                if (purrVoicePlayer != null)
                {
                    if (!purrVoicePlayer.isOwner) continue;
                    if(purrVoicePlayer.muted)
                    {
                        purrVoicePlayer.muted = false;
                    }
                }
            }
    }


    public void InitPushToTalk()
    {
        Debug.Log("PUSH TO TALK CALL");
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
        // mode push to talk ; dans ce cas le mode par defaut est activé, (a shit genre ca pose pas de probleme puisque les ghost vont etre demute des childs, peut etre il faut mettre le mode par defaut dans un update alors!!) (ils doivent etre mute avant de lancer cette fonction)
        Debug.Log("PUSH TO TALK PERFORME");
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

    public void MuteAllPlayerLocally() // ca devrait marcher en vrai non?
    {
        // mode pour deactive le proximity chat; personnes n'entend personne 
        Debug.Log("MUTE ALL PLAYER CALL");
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
}
