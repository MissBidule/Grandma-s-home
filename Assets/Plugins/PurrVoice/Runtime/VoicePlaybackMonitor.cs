using System;
using UnityEngine;

namespace PurrNet.Voice
{
    /// <summary>
    /// Static utility that tracks the current level of voice playback from remote players.
    /// Used by DuckingFilter to know when to attenuate the local microphone.
    /// </summary>
    public static class VoicePlaybackMonitor
    {
        private static float _playbackRms;
        private static float _lastReportTime;
        
        /// <summary>
        /// How quickly the tracked level decays when no new playback is reported (in seconds).
        /// After this duration of silence, the level drops to zero.
        /// </summary>
        private const float SILENCE_TIMEOUT = 0.06f;

        /// <summary>
        /// Current playback level (RMS). Returns 0 if no recent playback.
        /// </summary>
        public static float playbackLevel
        {
            get
            {
                float timeSince = Time.unscaledTime - _lastReportTime;
                if (timeSince > SILENCE_TIMEOUT)
                {
                    _playbackRms = 0f;
                    return 0f;
                }
                return _playbackRms;
            }
        }

        /// <summary>
        /// Call this from the playback path when voice audio samples are being played.
        /// Tracks the peak RMS level across all sources reporting in the same frame.
        /// </summary>
        public static void ReportPlayback(ArraySegment<float> samples)
        {
            if (samples.Array == null || samples.Count == 0)
                return;

            float sum = 0f;
            for (int i = 0; i < samples.Count; i++)
            {
                float s = samples.Array[samples.Offset + i];
                sum += s * s;
            }
            
            float rms = Mathf.Sqrt(sum / samples.Count);

            // Blend with previous value to avoid sudden drops between chunks,
            // but always accept higher peaks immediately
            _playbackRms = Mathf.Max(_playbackRms * 0.7f, rms);
            _lastReportTime = Time.unscaledTime;
        }

        /// <summary>
        /// Resets the monitor state. Useful for cleanup.
        /// </summary>
        public static void Reset()
        {
            _playbackRms = 0f;
            _lastReportTime = 0f;
        }
    }
}
