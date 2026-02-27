using System.Linq;
using System;
using PurrNet.Logging;
using PurrNet.Utils;
using UnityEngine;

namespace PurrNet.Voice
{
    public delegate ArraySegment<float> ProcessSamplesDelegate(ArraySegment<float> input, int frequency, params FilterLevel[] level);
    
    public partial class PurrVoicePlayer : NetworkIdentity
    {
        [SerializeField, PurrLock] private InputProvider _inputProvider;
        [SerializeField, PurrLock] private OutputProvider _outputProvider;
        
        /// <summary>
        /// Handles whether this PurrVoicePlayer is muted. If it's the local one, no audio will be sent. If it's a remote client, we won't replay their audio
        /// </summary>
        public bool muted;
        [SerializeField, PurrLock] private bool _enableLocalPlayback = false;
        [SerializeField, PurrLock, PurrShowIf("_enableLocalPlayback")] private OutputProvider _localOutputProvider;
        
        private NetworkAudioModule _transport;
        
        public bool usingLocalPlayback => _enableLocalPlayback;
        
        private IAudioInputSource micDevice => _inputProvider.input;
        
        public int inputFrequency => micDevice?.frequency ?? -1;
        public IVoiceOutput output => _outputProvider?.output;

        [SerializeField, PurrReadOnly] private string _currentDevice;

        /// <summary>
        /// Whenever you start playing audio from the microphone, this event will be invoked with the samples.
        /// </summary>
        public event Action<ArraySegment<float>> onReceivedSample;
        
        /// <summary>
        /// When you locally record audio from the microphone, this event will be invoked with the samples.
        /// </summary>
        public event Action<ArraySegment<float>> onLocalSample;

        private void Awake()
        {
            if (!_outputProvider)
            {
                PurrLogger.LogError($"Can't initialize PurrVoicePlayer with no output provider!", this);
                return;
            }

            if (_outputProvider is MultiAudioSourceVoiceProvider && GetComponent<AudioSource>() != null)
            {
                PurrLogger.LogError($"When using the MultiAudioSource Provider, PurrVoicePlayer cannot be on the same object as an audio source!", this);
                return;
            }

            _transport = new NetworkAudioModule(ProcessSamples);
            _transport.OnFrequencyChanged += OnFrequencyInitialized;
            _outputProvider.Init(_transport, ProcessSamples, FilterLevel.Receiver);
            _outputProvider.output.onEndPlayingSample += OnReplayingSample;
            FilterAwake();
        }

        private void OnFrequencyInitialized(int freq)
        {
            DebugAwake(freq);
            SetupVisualization(freq);
        }

        protected override void OnSpawned()
        {
            base.OnSpawned();

            if (isOwner)
            {
                _inputProvider.Init(this);
                SetupMicrophone();
                AudioDevices.onDevicesChanged += OnDevicesChanged;
            }
            else
            {
                SetupRemotePlayback();
            }
        }

        protected override void OnDespawned()
        {
            base.OnDespawned();
            Cleanup();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Cleanup();
        }

        private void OnValidate()
        {
            VisualizerValidate();

#if UNITY_EDITOR
            if (_localOutputProvider == _outputProvider && _outputProvider != null)
            {
                _localOutputProvider = null;
                PurrLogger.LogError($"Only one instance per output! If you want them to be the same, add 2 components of the same type", this);
            }
#endif
        }

        private void Cleanup()
        {
            CleanupVisualization();
            
            _inputProvider.Cleanup();
            _localOutputProvider?.output?.Stop();
            output?.Stop();
            
            AudioDevices.onDevicesChanged += OnDevicesChanged;
        }

        private void SetupMicrophone()
        {
            if (micDevice != null)
            {
                _currentDevice = micDevice.ToString();
                micDevice.onSampleReady += OnMicrophoneData;

                if (_enableLocalPlayback)
                {
                    var localSource = _localOutputProvider;
                    if (!_localOutputProvider)
                    {
                        PurrLogger.LogError($"Can't do local playback without a local output provider defined!", this);
                        return;
                    }
                    _localOutputProvider.Init(micDevice, ProcessSamplesLocal, FilterLevel.Server, FilterLevel.Receiver);
                    _localOutputProvider.output.onStartPlayingSample += DebugStreamedAudio;
                    _localOutputProvider.output.onEndPlayingSample += DebugStreamedAudioEnd;
                    _localOutputProvider.output.Start();
                }
                else
                {
                    micDevice.Start();
                }
                
                _transport.SetFrequency(micDevice.frequency);
            }
            else
            {
                PurrLogger.LogError($"No microphone devices found for {name}. Please connect a microphone.", this);
            }
        }

        public void ChangeMicrophone(IAudioInputSource mic)
        {
            if (mic == null || !isController)
                return;

            if (micDevice != null)
            {
                micDevice.onSampleReady -= OnMicrophoneData;
                micDevice.Stop();
            }
            _transport.Stop();
            
            _inputProvider.ChangeInput(mic);
            _currentDevice = micDevice?.ToString();
            _localOutputProvider?.SetInput(mic);
            micDevice?.Start();
            _transport.SetFrequency(mic.frequency);
            _transport.Start();
            
            if(micDevice != null)
                micDevice.onSampleReady += OnMicrophoneData;
        }

        private void SetupRemotePlayback()
        {
            output.Start();
        }

        private void OnAudioFilterRead(float[] data, int channels)
        {
            output?.HandleAudioFilterRead(data, channels);
            _localOutputProvider?.output?.HandleAudioFilterRead(data, channels);
        }

        private ArraySegment<float> ProcessSamples(ArraySegment<float> inputSamples, int frequency, params FilterLevel[] levels)
        {
            if (muted && levels.Contains(FilterLevel.Receiver))
                return MuteAudio(inputSamples);
            
            return DoProcessFilters(inputSamples, frequency, levels);
        }
        
        private ArraySegment<float> ProcessSamplesLocal(ArraySegment<float> inputSamples, int frequency, params FilterLevel[] levels)
        {
            if (muted)
                return MuteAudio(inputSamples);
            
            return DoLocalProcessFilters(inputSamples, frequency, levels);
        }

        private ArraySegment<float> MuteAudio(ArraySegment<float> inputSamples)
        {
            for (int i = 0; i < inputSamples.Count; i++)
            {
                inputSamples[i] = 0;
            }

            return inputSamples;
        }

        private void OnMicrophoneData(ArraySegment<float> samples)
        {
            if (muted)
                return;
            
            DebugMicrophoneDataPreProcessing(samples);
            samples = DoProcessFilters(samples, micDevice.frequency, FilterLevel.Sender);
            onLocalSample?.Invoke(samples);
            _transport.SendAudioChunk(samples);
            DebugMicrophoneDataPostProcessing(samples);
        }
        
        private void OnReplayingSample(ArraySegment<float> obj)
        {
            onReceivedSample?.Invoke(obj);
        }

        private void OnDevicesChanged()
        {
            if (!isOwner) return;
            if (!this || !gameObject) return;
            
            micDevice?.Stop();
            _localOutputProvider?.output?.Stop();
            SetupMicrophone();
        }
    }
}