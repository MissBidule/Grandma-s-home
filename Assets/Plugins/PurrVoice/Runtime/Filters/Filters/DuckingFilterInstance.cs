using System;
using UnityEngine;

namespace PurrNet.Voice
{
    public class DuckingFilterInstance : FilterInstance
    {
        private readonly DuckingFilter _def;
        
        // Current gain multiplier: 1 = no ducking, approaching 0 = fully ducked
        private float _currentGain = 1f;

        public DuckingFilterInstance(DuckingFilter def) => _def = def;

        public override void Process(ArraySegment<float> inputSamples, int frequency, float strength)
        {
            float playbackLevel = VoicePlaybackMonitor.playbackLevel;

            // Convert to dB for threshold comparison
            float playbackDb = 20f * Mathf.Log10(Mathf.Max(playbackLevel, 1e-10f));
            bool shouldDuck = playbackDb > _def.threshold;

            // Target gain: when ducking, reduce by duckingAmount scaled by filter strength
            // When not ducking, target is full volume (1.0)
            float targetGain = shouldDuck ? 1f - _def.duckingAmount * strength : 1f;

            float deltaTime = inputSamples.Count / (float)frequency;

            if (targetGain < _currentGain)
            {
                // Attack: move gain down quickly
                float attackRate = deltaTime / Mathf.Max(_def.attackTime, 0.0001f);
                _currentGain = Mathf.Max(targetGain, _currentGain - attackRate);
            }
            else
            {
                // Release: move gain up slowly
                float releaseRate = deltaTime / Mathf.Max(_def.releaseTime, 0.0001f);
                _currentGain = Mathf.Min(targetGain, _currentGain + releaseRate);
            }

            // Apply gain to all samples
            for (int i = 0; i < inputSamples.Count; i++)
                inputSamples.Array[inputSamples.Offset + i] *= _currentGain;
        }
    }
}
