using System;
using System.Linq;
using PurrNet.Logging;
using PurrNet.Packing;
using PurrNet.Transports;
using UnityEngine;

namespace PurrNet.Voice
{
    public class NetworkAudioModule : NetworkModule, IAudioInputSource
    {
        private float[] _chunkBuffer;
        private int _chunkSize;
        private int _bufferPos;
        private ProcessSamplesDelegate _processSamples;
        private SyncVar<int> _frequency = new(-1, ownerAuth:true);
        private OpusCodec _clientCodec;
        private OpusCodec _serverCodec;
        
        public event Action<int> OnFrequencyChanged 
        {
            add => _frequency.onChanged += value;
            remove => _frequency.onChanged -= value;
        }

        public int frequency => _frequency;

        public bool isRecording { get; private set; }

        public event Action<ArraySegment<float>> onSampleReady;

        private const int MAX_CHUNK_SIZE_BYTES = 900;


        public NetworkAudioModule(ProcessSamplesDelegate processSamples = null)
        {
            _frequency.onChanged += OnFrequencySet;
            _processSamples = processSamples;
        }

        private void OnFrequencySet(int newFreq)
        {
            int targetRate = GetClosestFrequency(newFreq);

            _chunkSize = targetRate / 50;
            _chunkBuffer = new float[_chunkSize];
            _bufferPos = 0;
            if(isClient)
                _clientCodec = new OpusCodec(targetRate, 1, _chunkSize);
            if(isServer)
                _serverCodec = new OpusCodec(targetRate, 1, _chunkSize);
        }

        private int GetClosestFrequency(int frequency)
        {
            int[] options = { 8000, 12000, 16000, 24000, 48000 };
            int closest = options[0];
            int minDiff = Math.Abs(frequency - closest);

            for (int i = 1; i < options.Length; i++)
            {
                int diff = Math.Abs(frequency - options[i]);
                if (diff < minDiff)
                {
                    minDiff = diff;
                    closest = options[i];
                }
            }

            return closest;
        }

        public void SetFrequency(int frequency)
        {
            if (!isController)
            {
                Debug.LogError($"Only the controller can set the frequency. Current controller: {owner}, current player: {localPlayer}");
                return;
            }
            
            _frequency.value = frequency;
        }

        public void SendAudioChunk(ArraySegment<float> segment)
        {
            if (!isOwner || _frequency.value < 0 || _chunkBuffer.Length <= 0 || _clientCodec == null) return;

            int offset = 0;
            while (offset < segment.Count)
            {
                int remaining = _chunkSize - _bufferPos;
                int copy = Math.Min(remaining, segment.Count - offset);
                if (copy <= 0) break;
                Array.Copy(segment.Array, segment.Offset + offset, _chunkBuffer, _bufferPos, copy);
                _bufferPos += copy;
                offset += copy;

                if (_bufferPos == _chunkSize)
                {
                    (parent as PurrVoicePlayer)?.DebugNetworkSentData(_chunkBuffer);
                    var encoded = _clientCodec.Encode(_chunkBuffer);

                    int encodedOffset = 0;
                    while (encodedOffset < encoded.Length)
                    {
                        int chunkLen = Math.Min(MAX_CHUNK_SIZE_BYTES, encoded.Length - encodedOffset);
                        byte[] chunk = new byte[chunkLen];
                        Array.Copy(encoded, encodedOffset, chunk, 0, chunkLen);
                        encodedOffset += chunkLen;
                        RpcSendAudio(chunk);
                    }

                    _bufferPos = 0;
                }
            }
        }

        [ServerRpc(channel: Channel.Unreliable, compressionLevel:CompressionLevel.Best)]
        private void RpcSendAudio(byte[] encoded)
        {
            if (_serverCodec == null)
            {
                //PurrLogger.LogError($"Server trying to process audio without a codec!");
                return;
            }
            
            var samples = _serverCodec.Decode(encoded);
            SendAudio_Internal(samples);
        }

        private void SendAudio_Internal(float[] samples)
        {
            if (_processSamples != null)
                samples = _processSamples(samples, _frequency, FilterLevel.Server).Array;

            var newEncoded = _serverCodec.Encode(samples);

            if (samples == null)
                return;
            
            (parent as PurrVoicePlayer)?.DebugServerProcessed(new ArraySegment<float>(samples));

            for (var i = 0; i < networkManager.players.Count; i++)
            {
                var player = networkManager.players[i];
                if (!parent.observers.Contains(player)) continue;
                if (owner == player)
                    continue;
                if (player == localPlayer)
                {
                    ReceiveAudio_Internal(samples);
                    continue;
                }

                TargetReceiveAudio(player, newEncoded);
            }
        }

        [TargetRpc(channel: Channel.Unreliable, compressionLevel:CompressionLevel.Best)]
        private void TargetReceiveAudio(PlayerID player, byte[] encoded)
        {
            if (_clientCodec == null)
            {
                //PurrLogger.LogError($"Client is trying to decode audio without a codec!");
                return;
            }
            
            var samples = _clientCodec.Decode(encoded);
            ReceiveAudio_Internal(samples);
        }

        private void ReceiveAudio_Internal(float[] samples)
        {
            (parent as PurrVoicePlayer)?.DebugReceived(new ArraySegment<float>(samples));
            onSampleReady?.Invoke(new ArraySegment<float>(samples));
        }
        
        public StartDeviceResult Start()
        {
            isRecording = true;
            return StartDeviceResult.Success;
        }

        public void Stop()
        {
            isRecording = false;
        }
    }
}
