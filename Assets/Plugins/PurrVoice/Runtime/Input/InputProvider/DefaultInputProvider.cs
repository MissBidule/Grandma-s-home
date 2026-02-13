using System;
using UnityEngine;

namespace PurrNet.Voice
{
    public class DefaultInputProvider : InputProvider
    {
        [Tooltip("This decides whether the PurrVoicePlayer will react to changes in the microphone devices. If false, it will not change the microphone when a new one is connected or disconnected.")]
        [SerializeField] private bool _reactToDeviceChanges = true;
        
        private IAudioInputSource _micDevice;

        public override IAudioInputSource input => _micDevice;
        public override event Action onDeviceChanged;

        private PurrVoicePlayer _purrVoicePlayer;

        public override void Init(PurrVoicePlayer purrVoicePlayer)
        {
            _purrVoicePlayer = purrVoicePlayer;
            _micDevice = AudioDevices.devices[0];
        }

        public override void Cleanup()
        {
            _micDevice?.Stop();
        }

        public override void ChangeInput(IAudioInputSource newInput)
        {
            if (_micDevice == newInput) return;

            Cleanup();
            _micDevice = newInput;
            onDeviceChanged?.Invoke();
        }

        private void OnEnable()
        {
            if(_reactToDeviceChanges)
                AudioDevices.onDevicesChanged += OnDevicesChanged;
        }

        private void OnDisable()
        {
            AudioDevices.onDevicesChanged -= OnDevicesChanged;
        }

        private void OnDevicesChanged()
        {
            if (!this || !gameObject) return;
            
            onDeviceChanged?.Invoke();
        }
    }
}
