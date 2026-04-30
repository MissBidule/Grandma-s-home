using PurrNet.Voice;
using UnityEngine;
using UnityEngine.InputSystem;

public class AudioManager : MonoBehaviour
{
    // Faut aussi gérer au début toujours mettre le bon mode (faut que le mode qui est écrit au début de partie s'applique vraiment)

    public void ProximityDefaultMode()
    {
        UnMutePlayers();
        UnMuteAllPlayerLocally();
















        /*bool isChild=false, isGhost=false;
        foreach (ChildController child in FindObjectsByType<ChildController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            PlayerInput input = child.GetComponent<PlayerInput>();

            if (input != null && input.enabled)
            {
                isChild=true;
                Debug.Log("pourquoiiii"+ child.gameObject);
            }
        }
        foreach (GhostController ghost in FindObjectsByType<GhostController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            PlayerInput input = ghost.GetComponent<PlayerInput>();

            if (input != null && input.enabled)
            {
                isGhost=true;
            }
        }

        if (isGhost == true)
        {
            ProximityByGhost();
        }
        if(isChild == true)
        {
            ProximityByChild();
        }*/

    }
    public void ProximityByChild() //faut faire selon si on est ghost ou child
    {
        Debug.Log("MUTE GHOST BY CHILD CALL");
            foreach(GameObject obj in FindObjectsByType<GameObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if(obj.layer == LayerMask.NameToLayer("Ghost"))
                {
                    AudioSource audioSource = obj.GetComponent<AudioSource>();
                    if (audioSource != null)
                    {
                        audioSource.volume=0;
                    }


                    /*PurrVoicePlayer purrVoicePlayer = obj.GetComponent<PurrVoicePlayer>();
                    if (purrVoicePlayer != null)
                    {
                        if(!purrVoicePlayer.muted)
                        {
                        purrVoicePlayer.muted=true; // faux => baisse le songs
                        }
                    }*/
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

    public void ProximityByGhost()
    {
        UnMutePlayers();
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

    public void UnMutePlayers() //A verifier
    {
        Debug.Log("UN MUTE SINGLE PLAYER CALL");
        foreach(GameObject obj in FindObjectsByType<GameObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                //AudioSource audioSource = obj.gameObject.GetComponent<AudioSource>();
                //if (audioSource != null)
                //{
                 //   audioSource.volume=0;
                //}

                PurrVoicePlayer purrVoicePlayer = obj.GetComponent<PurrVoicePlayer>();
                if (purrVoicePlayer != null)
                {
                    //if (!purrVoicePlayer.isOwner) continue;
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
        Debug.Log("MUTE ALL PLAYER LOCALY CALL");
        foreach(PlayerInput obj in FindObjectsByType<PlayerInput>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                AudioSource audioSource = obj.gameObject.GetComponent<AudioSource>();
                if (audioSource != null)
                {
                    audioSource.volume=0;
                }
                /*PurrVoicePlayer purrVoicePlayer = obj.GetComponent<PurrVoicePlayer>();
                if (purrVoicePlayer != null)
                {
                    if(!purrVoicePlayer.muted)
                    {
                        purrVoicePlayer.muted = true;
                    }
                }*/
            }
    }

    public void UnMuteAllPlayerLocally()
    {
        Debug.Log(" UN MUTE ALL PLAYER LOCALY CALL");
        foreach(PlayerInput obj in FindObjectsByType<PlayerInput>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                AudioSource audioSource = obj.gameObject.GetComponent<AudioSource>();
                if (audioSource != null)
                {
                    audioSource.volume=1;
                }
            }
    }
}
