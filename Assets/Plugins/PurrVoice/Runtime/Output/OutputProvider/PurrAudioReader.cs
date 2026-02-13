using System;
using UnityEngine;

/// <summary>
/// Lightweight bridge that exposes Unity’s
/// <see cref="MonoBehaviour.OnAudioFilterRead"/> callback as a public event.
/// Attach this to any GameObject with an <see cref="AudioSource"/> to let
/// external scripts subscribe to raw PCM data without needing their own
/// component on the AudioSource itself.
/// </summary>
public class PurrAudioReader : MonoBehaviour
{
    /// <summary>
    /// Fired for every audio buffer Unity produces.  
    /// • <paramref name="float[]"/> = interleaved samples.  
    /// • <paramref name="int"/>     = channel count (1 = mono, 2 = stereo).
    /// </summary>
    public Action<float[], int> OnAudioFilter;

    // Unity invokes this on the audio thread.
    private void OnAudioFilterRead(float[] data, int channels)
    {
        OnAudioFilter?.Invoke(data, channels);
    }
}
