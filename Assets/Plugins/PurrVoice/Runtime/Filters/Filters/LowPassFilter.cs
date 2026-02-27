using System;
using UnityEngine;

namespace PurrNet.Voice
{
    [CreateAssetMenu(fileName = "LowPassFilter", menuName = "PurrNet/Voice Chat/Filters/Low Pass Filter")]
    public class LowPassFilter : PurrAudioFilter
    {
        [Range(15f, 22000f)]
        public float cutoff = 5000f;
        
        public override FilterInstance CreateInstance()
        {
            return new LowPassFilterInstance(this);
        }
    }
}
