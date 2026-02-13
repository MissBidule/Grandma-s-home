using System;
using PurrNet.Voice;
using UnityEngine;

namespace PurrNet.Voice
{
    public partial class PurrVoicePlayer
    {
        public AudioVisualizer _micVisualizer { get; private set; }
        public AudioVisualizer _networkVisualizer { get; private set; }
        
        [Header("Audio Visualization")]
        [SerializeField] private float _visualAmplitudeScale = 3f;
        [SerializeField] private float _visualTimeScale = 1f;
        
        private void InitializeVisualizers(int frequency)
        {
#if UNITY_EDITOR
            _micVisualizer = new AudioVisualizer(3f, frequency);
            _networkVisualizer = new AudioVisualizer(3f, frequency);
            
            UpdateVisualizerSettings();
#endif
        }
        
        private void VisualizerValidate()
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                UpdateVisualizerSettings();
#endif
        }
        
        private void UpdateVisualizerSettings()
        {
#if UNITY_EDITOR
            if (_micVisualizer != null)
            {
                _micVisualizer.amplitudeScale = _visualAmplitudeScale;
                _micVisualizer.timeScale = _visualTimeScale;
            }
    
            if (_networkVisualizer != null)
            {
                _networkVisualizer.amplitudeScale = _visualAmplitudeScale;
                _networkVisualizer.timeScale = _visualTimeScale;
            }
#endif
        }
        
        private void SetupVisualization(int frequency)
        {
#if UNITY_EDITOR
            InitializeVisualizers(frequency);
            
            if (isOwner && micDevice != null)
            {
                micDevice.onSampleReady += OnMicVisualizationData;
            }
            
            if (_transport != null)
            {
                _transport.onSampleReady += OnNetworkVisualizationData;
            }
#endif
        }
        
        private void OnMicVisualizationData(ArraySegment<float> samples)
        {
#if UNITY_EDITOR
            _micVisualizer?.AddSamples(samples);
#endif
        }
        
        private void OnNetworkVisualizationData(ArraySegment<float> samples)
        {
#if UNITY_EDITOR
            _networkVisualizer?.AddSamples(samples);
#endif
        }
        
        private void CleanupVisualization()
        {
#if UNITY_EDITOR
            if (micDevice != null)
                micDevice.onSampleReady -= OnMicVisualizationData;
            
            if (_transport != null)
                _transport.onSampleReady -= OnNetworkVisualizationData;
#endif
        }
    }
}
