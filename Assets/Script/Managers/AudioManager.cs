using PurrNet.Voice;
using UnityEngine;
using PurrNet;

public class AudioManager : NetworkBehaviour
{
    void Update()
    {
        MuteGhostByChild();
    }

    void MuteGhostByChild()
    {
        if(ChildClientController.m_thePlayerIsAChild)
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
                        purrVoicePlayer.muted=true;
                        }
                    }
                }
            }
        }
    }
}
