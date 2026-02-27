using System;
using UnityEngine;

namespace PurrNet.Voice
{
    public class Device : IAudioInputSource
    {
        public DeviceID device { get; }

        public bool isRecording { get; private set; }

        public event Action<bool> onRecordingChanged;

        public event Action<ArraySegment<float>> onSampleReady;

        float[] _buffer = new float[1024];

        private int _frequency = 48000;

        public int frequency => _frequency;

        public Device(DeviceID device)
        {
            this.device = device;
        }

        public override string ToString()
        {
            return device.ToString();
        }

        public StartDeviceResult Start()
        {
            if (!AudioDevices.IsValidDevice(device))
                return StartDeviceResult.DeviceNotFound;

            if (!AudioDevices.hasPermission)
                return StartDeviceResult.NoPermission;

            if (isRecording)
                return StartDeviceResult.AlreadyRecording;

            isRecording = true;
            onRecordingChanged?.Invoke(true);

            AudioDevices.onPermissionChanged += PermissionsChanged;
            AudioDevices.onDevicesChanged += DevicesChanged;

            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                StartWebGL();
            }
            else
            {
                AudioDevices.onUpdate += UpdateUnity;
                StartUnity();
            }

            return StartDeviceResult.Success;
        }

        public void Stop()
        {
            if (!isRecording)
                return;

            isRecording = false;
            onRecordingChanged?.Invoke(false);

            AudioDevices.onPermissionChanged -= PermissionsChanged;
            AudioDevices.onDevicesChanged -= DevicesChanged;

            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                StopWebGL();
            }
            else
            {
                AudioDevices.onUpdate -= UpdateUnity;
                StopUnity();
            }
        }

        private void DevicesChanged()
        {
            bool valid = AudioDevices.IsValidDevice(device);

            switch (valid)
            {
                case false when isRecording:
                    Stop();
                    break;
                case true when !isRecording:
                    Start();
                    break;
            }
        }

        private void PermissionsChanged(bool hasPerms)
        {
            switch (hasPerms)
            {
                case false when isRecording:
                    Stop();
                    break;
                case true when !isRecording:
                    Start();
                    break;
            }
        }

        private AudioClip _clip;
        private int _lastSamplePos;

        private void StartUnity()
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            Microphone.GetDeviceCaps(device.id, out int minFreq, out int maxFreq);

            if (minFreq != 0 && maxFreq != 0)
                _frequency = Mathf.Clamp(_frequency, minFreq, maxFreq);

            _clip = Microphone.Start(device.id, true, 10, _frequency);
            _lastSamplePos = 0;
#endif
        }

        private void UpdateUnity()
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            if (!_clip)
                return;

            int currentSamplePos = Microphone.GetPosition(device.id);
            int totalSamples = _clip.samples;

            if (currentSamplePos < _lastSamplePos)
            {
                // Buffer wrapped: read from last to end, then 0 to current
                int samplesToEnd = totalSamples - _lastSamplePos;
                int samplesFromStart = currentSamplePos;

                // Ensure buffer is big enough
                if (_buffer.Length < samplesToEnd + samplesFromStart)
                    Array.Resize(ref _buffer, samplesToEnd + samplesFromStart);

                // Read end segment
                _clip.GetData(_buffer, _lastSamplePos);
                onSampleReady?.Invoke(new ArraySegment<float>(_buffer, 0, samplesToEnd));

                // Read start segment
                if (samplesFromStart > 0)
                {
                    _clip.GetData(_buffer, 0);
                    onSampleReady?.Invoke(new ArraySegment<float>(_buffer, 0, samplesFromStart));
                }
            }
            else
            {
                int samples = currentSamplePos - _lastSamplePos;
                if (samples > 0)
                {
                    if (_buffer.Length < samples)
                        Array.Resize(ref _buffer, samples);
                    _clip.GetData(_buffer, _lastSamplePos);
                    onSampleReady?.Invoke(new ArraySegment<float>(_buffer, 0, samples));
                }
            }

            _lastSamplePos = currentSamplePos;
#endif
        }

        private void StopUnity()
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            if (_clip)
            {
                Microphone.End(device.id);
                _clip = null;
            }
#endif
        }

        private void StartWebGL()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            AudioDevices.PurrVoice_StartRecording(device.id, 24000, AudioDevices.OnWebGLMicSamples);
#endif
        }

        private void StopWebGL()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            AudioDevices.PurrVoice_StopRecording(device.id);
#endif
        }

        internal void ReceivedWebGLFrequency(int frequency)
        {
            _frequency = frequency;
        }

        internal void ReceivedWebGLSamples(ArraySegment<float> arraySegment)
        {
            onSampleReady?.Invoke(arraySegment);
        }
    }
}
