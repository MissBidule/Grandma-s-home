using UnityEngine;

namespace PurrNet.Voice
{
    [CreateAssetMenu(fileName = "DuckingFilter", menuName = "PurrNet/Voice Chat/Filters/Ducking Filter")]
    public class DuckingFilter : PurrAudioFilter
    {
        [Header("Ducking - Reduces mic volume when others are speaking")]
        
        [Range(0f, 1f)]
        [Tooltip("How much to reduce the microphone volume when others are speaking. 0 = no reduction, 1 = full silence.")]
        public float duckingAmount = 0.8f;

        [Range(0.01f, 0.5f)]
        [Tooltip("How quickly ducking kicks in when voice playback is detected (in seconds).")]
        public float attackTime = 0.05f;

        [Range(0.01f, 1f)]
        [Tooltip("How quickly the mic volume recovers after playback stops (in seconds).")]
        public float releaseTime = 0.3f;

        [Range(-60f, -10f)]
        [Tooltip("Minimum playback level (in dB) required to trigger ducking. Lower values = more sensitive.")]
        public float threshold = -40f;

        public override FilterInstance CreateInstance()
        {
            return new DuckingFilterInstance(this);
        }
    }
}
