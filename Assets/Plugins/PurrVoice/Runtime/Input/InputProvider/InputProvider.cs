using System;
using UnityEngine;

namespace PurrNet.Voice
{
    public abstract class InputProvider : MonoBehaviour
    {
        /// <summary>
        /// The input that'll be used to provide audio data to the output provider.
        /// </summary>
        public abstract IAudioInputSource input { get; }

        /// <summary>
        /// Callback for when the input device changes, and the PurrVoicePlayer needs to update it's input source.
        /// </summary>
        public abstract event Action onDeviceChanged; 
        
        /// <summary>
        /// Initialization call. This is optional in this case, but a good indication for the provider to set up any necessary state or configuration. And after this call, the input should be ready to use.
        /// </summary>
        public virtual void Init(PurrVoicePlayer purrVoicePlayer) { }
        
        /// <summary>
        /// Called whenever the PurrVoicePlayer is doing cleanup
        /// </summary>
        public virtual void Cleanup() { }
        
        public virtual void ChangeInput(IAudioInputSource newInput) { }
    }
}
