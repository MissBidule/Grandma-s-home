using UnityEngine;

namespace PurrNet.Voice
{
    [CreateAssetMenu(fileName = "RNNoiseFilter", menuName = "PurrNet/Voice Chat/Filters/RNNoise Suppression")]
    public class RNNoiseFilter : PurrAudioFilter
    {
        [Header("RNNoise - Neural Network Noise Suppression")]

        [Range(0f, 1f)]
        [Tooltip("Voice activity threshold. Frames where RNNoise's VAD probability falls below " +
                 "this value will be fully silenced. 0 = no VAD gating (just denoise), " +
                 "higher values = more aggressive silencing of non-voice frames.")]
        public float vadThreshold = 0f;

        public override FilterInstance CreateInstance()
        {
            return new RNNoiseFilterInstance(this);
        }
    }
}
