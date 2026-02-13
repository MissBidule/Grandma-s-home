using PurrNet.Utils;
using UnityEngine;

namespace PurrNet.Voice
{
    public class AudioSourceVoiceProvider : OutputProvider
    {
        [SerializeField, PurrLock] private AudioSource _audioSource;
        
        private StreamedAudioClip _output;

        public override IVoiceOutput output => _output;

        public override void Init(IAudioInputSource inputSource, ProcessSamplesDelegate processSamples = null, params FilterLevel[] levels)
        {
            _output = new StreamedAudioClip();
            _output.Init(inputSource, processSamples, levels);
            _output.SetAudioSource(_audioSource);
        }

        public override void SetInput(IAudioInputSource input)
        {
            _output.SetInput(input);
        }

#if UNITY_EDITOR
        private void Reset()
        {
            if (!_audioSource)
                _audioSource = GetComponentInChildren<AudioSource>();

            if (_audioSource)
                _audioSource.dopplerLevel = 0;
        }
#endif
    }
}
