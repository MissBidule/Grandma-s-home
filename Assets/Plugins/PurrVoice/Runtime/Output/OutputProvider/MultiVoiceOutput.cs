using PurrNet.Voice;
using System;
using System.Collections.Generic;

namespace PurrVoice
{
    /// <summary>
    /// Composite <see cref="IVoiceOutput"/> that mirrors a single processed
    /// voice stream across multiple <see cref="StreamedAudioClip"/> targets.
    /// ▸ The *first* clip handles real playback via AudioSource.Play().  
    /// ▸ All remaining clips stay sample-accurate by receiving the same buffers
    ///   through <see cref="HandleAudioFilterRead"/>.
    /// </summary>
    public class MultiVoiceOutput : IVoiceOutput
    {
        /// <summary>All destination clips that will receive the duplicated audio.</summary>
        private readonly List<StreamedAudioClip> _targets;

        /// <summary>
        /// Gets the sample rate from the first clip (or –1 if there are none).
        /// </summary>
        public int frequency => _targets.Count > 0 ? _targets[0].frequency : -1;

        /// <summary>Raised just before a sample block starts playing.</summary>
        public event Action<ArraySegment<float>> onStartPlayingSample;

        /// <summary>Raised right after a sample block finishes playing.</summary>
        public event Action<ArraySegment<float>> onEndPlayingSample;

        /// <param name="targets">
        /// Clips that should all play the same voice stream.  
        /// Their order matters: element 0 is the “primary” clip.
        /// </param>
        public MultiVoiceOutput(List<StreamedAudioClip> targets)
        {
            _targets = targets;

            // Funnel per-clip callbacks into a single composite event.
            for (var i = 0; i < _targets.Count; i++)
            {
                var t = _targets[i];
                t.onStartPlayingSample += seg => onStartPlayingSample?.Invoke(seg);
                t.onEndPlayingSample += seg => onEndPlayingSample?.Invoke(seg);
            }
        }

        /// <summary>
        /// Passes the input source and optional DSP chain to every underlying clip.
        /// </summary>
        public void Init(IAudioInputSource input,
                         ProcessSamplesDelegate dsp = null,
                         params FilterLevel[] lvls)
        {
            _targets.ForEach(clip => clip.Init(input, dsp, lvls));
        }

        /// <summary>
        /// Starts playback on the primary clip and primes all others.
        /// </summary>
        public void Start()
        {
            if (_targets.Count == 0) return;

            _targets[0].Start();      // Real AudioSource.Play()
            for (int i = 1; i < _targets.Count; i++)
                _targets[i].SetupAudio(); // Allocates buffers without Play()
        }

        /// <summary>
        /// Stops playback and tears down auxiliary clips.
        /// </summary>
        public void Stop()
        {
            if (_targets.Count == 0) return;

            _targets[0].Stop();
            for (int i = 1; i < _targets.Count; i++)
                _targets[i].StopAudio();
        }

        /// <summary>
        /// Called on Unity’s audio thread.  
        /// Feeds the current mix buffer to every auxiliary clip so their ring
        /// buffers stay in lock-step with the primary one.
        /// </summary>
        public void HandleAudioFilterRead(float[] data, int channels)
        {
            if (_targets.Count == 0) return;

            // Skip index 0 – Unity already delivered this buffer internally.
            for (int i = 1; i < _targets.Count; i++)
                _targets[i].HandleAudioFilterRead(data, channels);
        }
    }
}
