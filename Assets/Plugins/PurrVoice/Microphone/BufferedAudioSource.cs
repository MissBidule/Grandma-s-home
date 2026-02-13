using System;
using System.Collections.Generic;
using UnityEngine;

namespace PurrNet.Voice
{
    public class BufferedAudioSource : IAudioInputSource
    {
        public int frequency { get; private set; }
        public bool isRecording { get; private set; }
        public event Action<ArraySegment<float>> onSampleReady;
        public event Action<ArraySegment<float>> onSamplesBuffered;

        private Queue<float> _audioQueue;
        private int _chunkSize;
        private int _minQueueSize;
        private int _maxQueueSize;
        private float _pushTimer;
        private float[] _tempChunk;

        public BufferedAudioSource(int frequency, float bufferSeconds = 0.3f, float chunkSeconds = 0.02f)
        {
            this.frequency = frequency;
            _chunkSize = Mathf.RoundToInt(chunkSeconds * frequency);
            _minQueueSize = Mathf.RoundToInt(bufferSeconds * frequency * 0.5f);
            _maxQueueSize = Mathf.RoundToInt(bufferSeconds * frequency);
            _audioQueue = new Queue<float>(_maxQueueSize);
            _tempChunk = new float[_chunkSize];
        }

        public void PushAudioData(ArraySegment<float> samples)
        {
            onSamplesBuffered?.Invoke(samples);
            for (int i = 0; i < samples.Count; i++)
            {
                if (_audioQueue.Count >= _maxQueueSize)
                    _audioQueue.Dequeue();
                
                _audioQueue.Enqueue(samples.Array[samples.Offset + i]);
            }
        }

        public StartDeviceResult Start()
        {
            if (isRecording) return StartDeviceResult.AlreadyRecording;
            isRecording = true;
            return StartDeviceResult.Success;
        }

        public void Stop()
        {
            isRecording = false;
            _audioQueue.Clear();
            _pushTimer = 0;
        }

        public void TickUpdate(float deltaTime)
        {
            if (!isRecording) return;

            _pushTimer += deltaTime;
            while (_pushTimer >= (float)_chunkSize / frequency)
            {
                _pushTimer -= (float)_chunkSize / frequency;

                if (_audioQueue.Count < _minQueueSize)
                {
                    Array.Clear(_tempChunk, 0, _chunkSize);
                    onSampleReady?.Invoke(new ArraySegment<float>(_tempChunk, 0, _chunkSize));
                    return;
                }

                int samplesAvailable = Mathf.Min(_chunkSize, _audioQueue.Count);
                
                for (int i = 0; i < samplesAvailable; i++)
                {
                    _tempChunk[i] = _audioQueue.Dequeue();
                }

                if (samplesAvailable < _chunkSize)
                {
                    for (int i = samplesAvailable; i < _chunkSize; i++)
                    {
                        _tempChunk[i] = 0f;
                    }
                }

                onSampleReady?.Invoke(new ArraySegment<float>(_tempChunk, 0, _chunkSize));
            }
        }
    }
}