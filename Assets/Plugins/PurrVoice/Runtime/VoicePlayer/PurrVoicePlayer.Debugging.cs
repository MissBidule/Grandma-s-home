using System;
using UnityEngine;

namespace PurrNet.Voice
{
    public partial class PurrVoicePlayer
    {
#if UNITY_EDITOR
        [HideInInspector] public AudioVisualizer micInputVisualizer;
        [HideInInspector] public AudioVisualizer senderProcessedVisualizer;
        [HideInInspector] public AudioVisualizer networkSentVisualizer;
        [HideInInspector] public AudioVisualizer serverProcessedVisualizer;
        [HideInInspector] public AudioVisualizer receivedVisualizer;
        [HideInInspector] public AudioVisualizer streamedAudioVisualizerStart;
        [HideInInspector] public AudioVisualizer streamedAudioVisualizerEnd;

        public bool enableDebugVisualization => _purrVoicePlayerDebug != null;
        private PurrVoicePlayerDebug _purrVoicePlayerDebug;
#endif
        
        private void DebugAwake(int frequency)
        {
#if UNITY_EDITOR
            TryGetComponent(out _purrVoicePlayerDebug);
            
            if (!enableDebugVisualization)
                return;
            
            micInputVisualizer = new AudioVisualizer(_purrVoicePlayerDebug.timeWindow, frequency);
            senderProcessedVisualizer = new AudioVisualizer(_purrVoicePlayerDebug.timeWindow, frequency);
            networkSentVisualizer = new AudioVisualizer(_purrVoicePlayerDebug.timeWindow, frequency);
            serverProcessedVisualizer = new AudioVisualizer(_purrVoicePlayerDebug.timeWindow, frequency);
            receivedVisualizer = new AudioVisualizer(_purrVoicePlayerDebug.timeWindow, frequency);
            streamedAudioVisualizerStart = new AudioVisualizer(_purrVoicePlayerDebug.timeWindow, frequency);
            streamedAudioVisualizerEnd = new AudioVisualizer(_purrVoicePlayerDebug.timeWindow, frequency);

            //_networkBuffer.onSamplesBuffered += DebugBufferedData;
            output.onStartPlayingSample += DebugStreamedAudio;
            output.onEndPlayingSample += DebugStreamedAudioEnd;
#endif
        }
        
        private void DebugMicrophoneDataPreProcessing(ArraySegment<float> samples)
        {
#if UNITY_EDITOR
            if (enableDebugVisualization && isOwner)
                micInputVisualizer?.AddSamples(samples);
#endif
        }

        private void DebugMicrophoneDataPostProcessing(ArraySegment<float> samples)
        {
#if UNITY_EDITOR
            if (enableDebugVisualization && isOwner)
                senderProcessedVisualizer?.AddSamples(samples);
#endif
        }
        
        internal void DebugNetworkSentData(ArraySegment<float> samples)
        {
#if UNITY_EDITOR
            if (enableDebugVisualization && isOwner)
                networkSentVisualizer?.AddSamples(samples);
#endif
        }
        
        internal void DebugServerProcessed(ArraySegment<float> samples)
        {
#if UNITY_EDITOR
            if (enableDebugVisualization && isServer)
                serverProcessedVisualizer?.AddSamples(samples);
#endif
        }
        
        internal void DebugReceived(ArraySegment<float> samples)
        {
#if UNITY_EDITOR
            if (enableDebugVisualization && !isOwner)
                receivedVisualizer?.AddSamples(samples);
#endif
        }
        
        private void DebugStreamedAudio(ArraySegment<float> samples)
        {
#if UNITY_EDITOR
            if (enableDebugVisualization)
                streamedAudioVisualizerStart?.AddSamples(samples);
#endif
        }
        
        private void DebugStreamedAudioEnd(ArraySegment<float> samples)
        {
#if UNITY_EDITOR
            if (enableDebugVisualization)
                streamedAudioVisualizerEnd?.AddSamples(samples);
#endif
        }
    }
}
