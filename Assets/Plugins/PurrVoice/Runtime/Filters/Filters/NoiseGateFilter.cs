using UnityEngine;

namespace PurrNet.Voice
{
    [CreateAssetMenu(fileName = "NoiseFilter", menuName = "PurrNet/Voice Chat/Filters/Noise Filter")]
    public class NoiseGateFilter : PurrAudioFilter
    {
        [Header("Noise Gate")]
        [Range(-60f, -10f)]
        public float noiseGateThreshold = -40f;
        
        [Range(0.01f, 1f)]
        public float gateAttackTime = 0.15f;
        
        [Range(0.01f, 1f)]
        public float gateReleaseTime = 0.3f;
        
        public override FilterInstance CreateInstance()
        {
            return new NoiseGateFilterInstance(this);
        }
    }
}