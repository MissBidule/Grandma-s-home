using UnityEngine;

namespace PurrNet.Voice
{
    public abstract class PurrAudioFilter : ScriptableObject
    {
        public abstract FilterInstance CreateInstance();
    }
}
