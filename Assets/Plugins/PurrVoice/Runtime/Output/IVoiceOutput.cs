using System;
using UnityEngine;

namespace PurrNet.Voice
{
    public interface IVoiceOutput
    {
        /// <summary>
        /// Callback for when samples entered the audio handling
        /// </summary>
        public event Action<ArraySegment<float>> onStartPlayingSample;
        
        /// <summary>
        /// Callback for when samples are finished audio handling
        /// </summary>
        public event Action<ArraySegment<float>> onEndPlayingSample;

        /// <summary>
        /// Current frequency of playback
        /// </summary>
        public int frequency { get; }

        /// <summary>
        /// Used for initializing the output with necessary context
        /// </summary>
        /// <param name="source"></param>
        /// <param name="inputSource"></param>
        /// <param name="processSamples"></param>
        /// <param name="levels"></param>
        public void Init(IAudioInputSource inputSource, ProcessSamplesDelegate processSamples = null, params FilterLevel[] levels);

        public void Start();
        public void Stop();
        
        public void HandleAudioFilterRead(float[] data, int channels);
    }
}
