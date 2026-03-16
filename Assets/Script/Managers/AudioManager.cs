using PurrNet.Voice;
using UnityEngine;

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
                        purrVoicePlayer.muted=true;
                        }
                    }
                }
            }
    }
}
