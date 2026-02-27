using System;
using System.Collections.Generic;
using UnityEngine;

namespace PurrNet.Voice
{
    public class AudioVisualizer
    {
        private Queue<float> _sampleHistory = new Queue<float>();
        private int _maxSamples;
        private int _frequency;
        private int _sampleCounter;
        private int _downsampleRate;
        
        public float amplitudeScale { get; set; } = 1f;
        public float timeScale { get; set; } = 1f;
    
        public AudioVisualizer(float timeWindow = 3f, int frequency = 48000, int targetVisualizationSamples = 300)
        {
            _frequency = frequency;
            
            int totalSamplesInWindow = Mathf.RoundToInt(timeWindow * frequency);
            _downsampleRate = Mathf.Max(1, totalSamplesInWindow / targetVisualizationSamples);
            _maxSamples = totalSamplesInWindow / _downsampleRate;
        }
    
        public void AddSample(float sample)
        {
            _sampleCounter++;
            
            if (_sampleCounter % _downsampleRate == 0)
            {
                float scaledSample = sample;
                
                scaledSample = sample * amplitudeScale;
                
                _sampleHistory.Enqueue(scaledSample);
                
                while (_sampleHistory.Count > GetAdjustedMaxSamples())
                    _sampleHistory.Dequeue();
            }
        }
    
        public void AddSamples(ArraySegment<float> samples)
        {
            for (int i = 0; i < samples.Count; i++)
            {
                AddSample(samples.Array[samples.Offset + i]);
            }
        }
        
        private int GetAdjustedMaxSamples()
        {
            return Mathf.RoundToInt(_maxSamples * timeScale);
        }
    
        public float[] GetSamples()
        {
            return _sampleHistory.ToArray();
        }
        
        public float GetMaxAmplitude()
        {
            float max = 0f;

            var _history = _sampleHistory.ToArray();
            foreach (var sample in _history)
            {
                float abs = Mathf.Abs(sample);
                if (abs > max) max = abs;
            }
            return max;
        }

        public float GetRMSAmplitude()
        {
            if (_sampleHistory.Count == 0) return 0f;

            var _history = _sampleHistory.ToArray();
            float sum = 0f;
            foreach (var sample in _history)
            {
                sum += sample * sample;
            }
            return Mathf.Sqrt(sum / _sampleHistory.Count);
        }
        
        public float GetCurrentTimeWindow()
        {
            return (_sampleHistory.Count * _downsampleRate) / (float)_frequency;
        }
    
        public void Clear()
        {
            _sampleHistory.Clear();
            _sampleCounter = 0;
        }
    }
}