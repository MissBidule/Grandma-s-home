using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PurrNet.Voice.LipSync
{
    public class PurrLipSync : uLipSync.uLipSync
    {
        [SerializeField] PurrVoicePlayer _purrVoicePlayer;

        public event Action<string> onPhonemeChanged;
        private readonly Queue<string> _phonemeHistory = new Queue<string>();

        private float _lastSampleTime;
        const float SAMPLE_INTERVAL = 0.02f;

        protected override void OnEnable()
        {
            base.OnEnable();
            if (_purrVoicePlayer)
            {
                _purrVoicePlayer.onLocalSample += OnLocalSample;
                _purrVoicePlayer.onReceivedSample += OnReceivedSample;
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (_purrVoicePlayer)
            {
                _purrVoicePlayer.onLocalSample -= OnLocalSample;
                _purrVoicePlayer.onReceivedSample -= OnReceivedSample;
            }
        }

        private string GetPhoneme()
        {
            if (result.volume < 0.1f)
            {
                _phonemeHistory.Enqueue("");
                return string.Empty;
            }

            _phonemeHistory.Enqueue(result.phoneme);
            while (_phonemeHistory.Count > 5)
                _phonemeHistory.Dequeue();

            var mostFrequentPhoneme = _phonemeHistory
                .GroupBy(p => p)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefault();

            return mostFrequentPhoneme;
        }
        
        private void LateUpdate()
        {
            if (Time.time - _lastSampleTime < SAMPLE_INTERVAL)
                return;

            _lastSampleTime = Time.time;

            var f = GetPhoneme();
        
            OnPhonemeChanged(f);
            onPhonemeChanged?.Invoke(f);
        }

        private void OnLocalSample(ArraySegment<float> samples)
        {
            OnDataReceived(samples, 1);
        }

        private void OnReceivedSample(ArraySegment<float> samples)
        {
            OnDataReceived(samples, 1);
        }

        protected override void OnAudioFilterRead(float[] input, int channels)
        {
            if (_purrVoicePlayer)
                return;
            base.OnAudioFilterRead(input, channels);
        }
        
        protected virtual void OnPhonemeChanged(string phoneme)
        {
        }
    }
}
