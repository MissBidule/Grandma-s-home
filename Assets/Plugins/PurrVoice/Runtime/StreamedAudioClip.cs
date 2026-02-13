using System;
using UnityEngine;

namespace PurrNet.Voice
{
    public class StreamedAudioClip : IVoiceOutput
    {
        public AudioSource source;

        public event Action<ArraySegment<float>> onStartPlayingSample;
        public event Action<ArraySegment<float>> onEndPlayingSample;

        private ProcessSamplesDelegate _processSamples;
        private FilterLevel[] _levels;

        public float playbackOffsetInSeconds = 0.1f;
        public IAudioInputSource inputSource;

        public int frequency => inputSource?.frequency ?? -1;

        private readonly System.Collections.Concurrent.ConcurrentQueue<float> _buffer = new();
        private int _bufferedSampleTarget;
        private bool _isReady;
        private bool _shouldPlay;

        private AudioClip _streamClip;
        private bool _audioSetup;
        private int _lastWritePosition;
        
        private int _clipLen;
        private int _writeHead;
        private int _desiredLag;

        public void Init(IAudioInputSource inputSource, ProcessSamplesDelegate processSamples = null, params FilterLevel[] levels)
        {
            this.inputSource = inputSource;
            this._processSamples = processSamples;
            _levels = levels;
            NetworkManager.main.onTick += OnTick;
        }

        public void SetAudioSource(AudioSource source)
        {
            this.source = source;
        }

        public void SetInput(IAudioInputSource mic)
        {
            if (mic == null)
                return;

            if (inputSource != null)
            {
                inputSource.Stop();
                inputSource.onSampleReady -= OnSampleReady;
            }
            
            inputSource = mic;
            inputSource.onSampleReady += OnSampleReady;
            inputSource.Start();
        }

        public void Start()
        {
            if (inputSource == null || inputSource.isRecording)
                return;

            if (inputSource.Start() != StartDeviceResult.Success)
                return;

            SetupAudio();
        }

        public void SetupAudio()
        {
            inputSource.onSampleReady += OnSampleReady;
            source.loop = true;
            source.playOnAwake = false;

            _isReady = false;
            _audioSetup = false;
            _bufferedSampleTarget = 0;
            _lastWritePosition = 0;
        }

        private void EnsureAudioClipCreated()
        {
            if (_audioSetup || frequency <= 0) return;
            int sr = AudioSettings.outputSampleRate;
            _clipLen = sr; 
            _streamClip = AudioClip.Create("StreamedVoice", _clipLen, 1, sr, false);
            source.clip = _streamClip;
            AudioSettings.GetDSPBufferSize(out int dsp, out int num);
            _desiredLag = Mathf.CeilToInt(playbackOffsetInSeconds * sr) + (dsp * num);
            _writeHead = 0;
            _audioSetup = true;
        }

        public void Stop()
        {
            if (inputSource == null || !inputSource.isRecording)
                return;
            
            inputSource.Stop();
            StopAudio();
        }

        public void StopAudio()
        {
            inputSource.onSampleReady -= OnSampleReady;
            if (source && source.isPlaying) source.Stop();
            _audioSetup = false;
            _isReady = false;
        }

        public void HandleAudioFilterRead(float[] data, int channels)
        {
        }

        private void OnSampleReady(ArraySegment<float> data)
        {
            EnsureAudioClipCreated();
            if (_processSamples != null) data = _processSamples(data, frequency, _levels);
            
            // Report playback activity so ducking filters can detect when voice is playing
            VoicePlaybackMonitor.ReportPlayback(data);
            
            onStartPlayingSample?.Invoke(data);

            int inRate = frequency;
            int outRate = AudioSettings.outputSampleRate;
            int outCount = Mathf.CeilToInt(data.Count * (outRate / (float)inRate));
            var tmp = System.Buffers.ArrayPool<float>.Shared.Rent(outCount);
            for (int i = 0; i < outCount; i++)
            {
                float t = i * (inRate / (float)outRate);
                int t0 = Mathf.FloorToInt(t);
                int t1 = Mathf.Min(t0 + 1, data.Count - 1);
                tmp[i] = Mathf.Lerp(data[t0], data[t1], t - t0);
            }
            int remaining = outCount;
            int srcOff = 0;
            while (remaining > 0)
            {
                int chunk = Mathf.Min(remaining, _clipLen - _writeHead);
                var slice = new float[chunk];
                Array.Copy(tmp, srcOff, slice, 0, chunk);
                _streamClip.SetData(slice, _writeHead);
                _writeHead = (_writeHead + chunk) % _clipLen;
                remaining -= chunk;
                srcOff += chunk;
            }
            System.Buffers.ArrayPool<float>.Shared.Return(tmp);
            
            onEndPlayingSample?.Invoke(data);
            
            if (!_isReady)
            {
                int buffered = (_writeHead - source.timeSamples + _clipLen) % _clipLen;
                if (buffered >= _desiredLag)
                {
                    int startPos = (_writeHead - _desiredLag + _clipLen) % _clipLen;
                    source.timeSamples = startPos;
                    source.Play();
                    _isReady = true;
                }
            }
        }
        
        public void OnTick(bool asServer)
        {
            if (!_audioSetup || _streamClip == null || source == null) return;

            int ahead = (_writeHead - source.timeSamples + _clipLen) % _clipLen;
            if (ahead < _desiredLag) WriteZeros(_desiredLag - ahead);

            if (!_isReady && ahead >= _desiredLag)
            {
                int startPos = (_writeHead - _desiredLag + _clipLen) % _clipLen;
                source.timeSamples = startPos;
                source.Play();
                _isReady = true;
            }

            if (_shouldPlay)
            {
                _shouldPlay = false;
                if (!source.isPlaying) source.Play();
            }
        }
        
        private void WriteZeros(int count)
        {
            while (count > 0)
            {
                int chunk = Mathf.Min(count, _clipLen - _writeHead);
                var slice = System.Buffers.ArrayPool<float>.Shared.Rent(chunk);
                Array.Clear(slice, 0, chunk);
                _streamClip.SetData(slice, _writeHead);
                _writeHead = (_writeHead + chunk) % _clipLen;
                System.Buffers.ArrayPool<float>.Shared.Return(slice);
                count -= chunk;
            }
        }
    }
}