using System;
using PurrNet.Voice;
using UnityEngine;

namespace PurrNet.Voice
{
    public class LowPassFilterInstance : FilterInstance
    {
        public LowPassFilterInstance(LowPassFilter filter) => _def = filter;
        
        private readonly LowPassFilter _def;
        
        private double _y;
        private double _alpha;
        private float _lastCutoff = -1f;
        private bool _initialized;
        private const float MIN_CUTOFF = 15f;
        private const float MAX_CUTOFF = 22000f;
        private float _lastValue;

        public override void Process(ref float sample, int frequency, float strength)
        {
            var cutoff = Mathf.Clamp(_def.cutoff, MIN_CUTOFF, Mathf.Min(MAX_CUTOFF, frequency * 0.45f));
            cutoff = Mathf.Lerp(MAX_CUTOFF, cutoff,strength);
            
            bool needsRecalc = !_initialized || Math.Abs(cutoff - _lastCutoff) > 0.001f;
            
            if (needsRecalc)
            {
                var dt = 1.0 / frequency;
                var rc = 1.0 / (2.0 * Math.PI * cutoff);
                _alpha = dt / (rc + dt);
                
                _lastCutoff = cutoff;
            }
            
            float xi = sample;
            _y = _y + _alpha * (xi - _y);
            
            sample = (float)Math.Clamp(_y, -1.0, 1.0);
        }
    }
}