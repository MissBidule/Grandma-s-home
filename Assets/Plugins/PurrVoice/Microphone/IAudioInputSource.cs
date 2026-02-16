using System;
using UnityEngine;

namespace PurrNet.Voice
{
    public interface IAudioInputSource
    {
        int frequency { get; }
        bool isRecording { get; }
        event Action<ArraySegment<float>> onSampleReady;
        StartDeviceResult Start();
        void Stop();
    }

}
