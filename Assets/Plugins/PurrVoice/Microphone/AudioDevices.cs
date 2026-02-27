using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOT;
using JetBrains.Annotations;
using UnityEngine;

namespace PurrNet.Voice
{
    public class AudioDevices : MonoBehaviour
    {
        private static AudioDevices _instance;

        static readonly List<Device> _devices = new ();

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void PurrVoice_RequestPermission();

        [DllImport("__Internal")]
        private static extern void PurrVoice_EnumerateDevices();

        [DllImport("__Internal")]
        internal static extern void PurrVoice_StartRecording(string deviceId, int sampleRate, OnWebGLMicSamplesDelegate callback);

        [DllImport("__Internal")]
        internal static extern void PurrVoice_StopRecording(string deviceId);
#endif

        public static event Action<bool> onPermissionChanged;
        public static event Action onDevicesChanged;
        internal static event Action onUpdate;

        private bool _hasPermission =
#if UNITY_WEBGL && !UNITY_EDITOR
            false;
#else
            true;
#endif

        private bool _hasRequestedPermission;

        readonly List<Device> _pool = new ();

        /// <summary>
        /// Returns true if the microphone permission has been granted.
        /// </summary>
        public static bool hasPermission
        {
            get
            {
                if (_instance)
                    return _instance._hasPermission;
                return false;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void InitScript()
        {
            _devices.Clear();

            var microphone = new GameObject("PurrMicrophone");
            _instance = microphone.AddComponent<AudioDevices>();
            DontDestroyOnLoad(microphone);

            microphone.hideFlags = HideFlags.HideAndDontSave;
        }

        public static IReadOnlyList<Device> devices
        {
            get
            {
                if (Application.platform == RuntimePlatform.WebGLPlayer)
                {
                    if (_instance && !_instance._hasRequestedPermission)
                    {
#if UNITY_WEBGL && !UNITY_EDITOR
                        PurrVoice_RequestPermission();
#endif
                        _instance._hasRequestedPermission = true;
                    }
                }
                else
                {
                    QueryDevicesUnity();
                }

                return _devices;
            }
        }

        [UsedImplicitly]
        public void OnMicPermission(int granted)
        {
            bool isGranted = granted == 1;

            if (_hasPermission == isGranted)
                return;

            _hasPermission = isGranted;

            onPermissionChanged?.Invoke(isGranted);

            if (isGranted)
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                PurrVoice_EnumerateDevices();
#endif
            }
            else
            {
                bool wasEmpty = _devices.Count == 0;
                _devices.Clear();
                bool isEmpty = _devices.Count == 0;
                if (wasEmpty != isEmpty)
                    onDevicesChanged?.Invoke();
            }
        }

        static float[] _buffer = new float[1024];

        internal delegate void OnWebGLMicSamplesDelegate(IntPtr ptr, int sampleCount, int sampleRate, IntPtr deviceIdPtr);

        [UsedImplicitly, MonoPInvokeCallback(typeof(OnWebGLMicSamplesDelegate))]
        internal static void OnWebGLMicSamples(IntPtr ptr, int sampleCount, int sampleRate, IntPtr deviceIdPtr)
        {
            string deviceId = Marshal.PtrToStringUTF8(deviceIdPtr);

            if (_buffer.Length < sampleCount)
                Array.Resize(ref _buffer, sampleCount);
            Marshal.Copy(ptr, _buffer, 0, sampleCount);

            for (int i = 0; i < _devices.Count; i++)
            {
                if (_devices[i].device.id == deviceId)
                {
                    _devices[i].ReceivedWebGLFrequency(sampleRate);
                    _devices[i].ReceivedWebGLSamples(new ArraySegment<float>(_buffer, 0, sampleCount));
                    return;
                }
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            PurrVoice_StopRecording(deviceId);
#endif
        }

        private bool TryDepool(string id, out Device device)
        {
            for (int i = 0; i < _pool.Count; i++)
            {
                if (_pool[i].device.id == id)
                {
                    device = _pool[i];
                    _pool.RemoveAt(i);
                    return true;
                }
            }

            device = default;
            return false;
        }

        private void ClearPool()
        {
            for (int i = 0; i < _pool.Count; i++)
            {
                _pool[i].Stop();
            }

            _pool.Clear();
        }

        [UsedImplicitly]
        public void OnMicDevices(string devices)
        {
            _pool.AddRange(_devices);
            _devices.Clear();

            var parsed = JsonUtility.FromJson<DeviceIDArray>(devices);
            for (int i = 0; i < parsed.items.Length; i++)
            {
                if (TryDepool(parsed.items[i].id, out var d))
                {
                    _devices.Add(d);
                    continue;
                }

                _devices.Add(new Device(parsed.items[i]));
            }

            ClearPool();
            onDevicesChanged?.Invoke();
        }

        private void LateUpdate()
        {
            if (Application.platform != RuntimePlatform.WebGLPlayer)
                QueryDevicesUnity();
            onUpdate?.Invoke();
        }

        /// <summary>
        /// Requests permission to use the microphone if necessary.
        /// </summary>
        public static void RequestPermission()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (Application.platform == RuntimePlatform.WebGLPlayer && !hasPermission)
                PurrVoice_RequestPermission();
#endif
        }

        static void QueryDevicesUnity()
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            var actual = Microphone.devices;
            var current = _devices;

            if (actual.Length != current.Count)
            {
                PopulateUnityMics(actual);
                onDevicesChanged?.Invoke();
            }
            else
            {
                for (int i = 0; i < actual.Length; i++)
                {
                    if (actual[i] != current[i].device.id)
                    {
                        PopulateUnityMics(actual);
                        onDevicesChanged?.Invoke();
                        break;
                    }
                }
            }
#endif
        }

        private static void PopulateUnityMics(string[] actual)
        {
            if (!_instance)
                return;

            _instance._pool.AddRange(_devices);
            _devices.Clear();
            for (int i = 0; i < actual.Length; i++)
            {
                var device = new DeviceID(actual[i], actual[i]);
                if (_instance.TryDepool(device.id, out var d))
                {
                    _devices.Add(d);
                    continue;
                }

                _devices.Add(new Device(device));
            }
            _instance.ClearPool();
        }

        public static bool IsValidDevice(DeviceID device)
        {
            if (device.id == null)
                return false;

            for (int i = 0; i < _devices.Count; i++)
            {
                if (_devices[i].device.id == device.id)
                    return true;
            }

            return false;
        }
    }
}
