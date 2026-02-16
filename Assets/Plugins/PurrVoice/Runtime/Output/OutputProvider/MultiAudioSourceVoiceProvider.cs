using PurrNet.Logging;
using PurrVoice;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditorInternal;
#endif

namespace PurrNet.Voice
{
    /// <summary>
    /// <see cref="OutputProvider"/> that clones a voice stream into every
    /// <see cref="AudioSource"/> assigned in the Inspector.  
    /// Internally wraps each AudioSource in a <see cref="StreamedAudioClip"/>
    /// and then groups them under a single <see cref="MultiVoiceOutput"/>.
    /// </summary>
    public class MultiAudioSourceVoiceProvider : OutputProvider
    {
        // AudioSources that will receive duplicated voice audio.
        [SerializeField] private List<AudioSource> _audioSources = new();

        private List<StreamedAudioClip> _clips;
        private MultiVoiceOutput _output;

        /// <inheritdoc/>
        public override IVoiceOutput output => _output;

        /// <summary>
        /// Builds one <see cref="StreamedAudioClip"/> per AudioSource,
        /// ensures each has a <see cref="PurrAudioReader"/>, and finally
        /// wraps them all in a <see cref="MultiVoiceOutput"/>.
        /// </summary>
        public override void Init(IAudioInputSource input,
                                  ProcessSamplesDelegate dsp = null,
                                  params FilterLevel[] lvls)
        {
            _clips = new List<StreamedAudioClip>(_audioSources.Count);

            foreach (AudioSource src in _audioSources)
            {
                if (!src) continue; // Ignore empty slots.

                // Wrap the AudioSource so we can push samples manually.
                var clip = new StreamedAudioClip();
                clip.SetAudioSource(src);
                clip.Init(input, dsp, lvls);
                _clips.Add(clip);

                // Forward raw sample callbacks via PurrAudioReader.
                if (src.TryGetComponent<PurrAudioReader>(out var reader))
                {
                    AddCallback(reader, clip);
                    continue;
                }

                // Editor feedback: helper component is missing.
                PurrLogger.LogSimplerError(
                    $"PurrAudioReader not found on '{src.gameObject.name}'. " +
                    $"Please add the component so audio can be duplicated.");
            }

            // Single composite output for the rest of the voice system.
            _output = new MultiVoiceOutput(_clips);
        }

        /// <summary>
        /// Subscribes the clip to the reader’s audio callback so it stays
        /// sample-synchronized with the primary clip.
        /// </summary>
        private void AddCallback(PurrAudioReader reader, StreamedAudioClip clip)
        {
            reader.OnAudioFilter += clip.HandleAudioFilterRead;
        }

        /// <summary>
        /// Propagates a new microphone (or any <see cref="IAudioInputSource"/>)
        /// reference to every existing clip.
        /// </summary>
        public override void SetInput(IAudioInputSource mic)
        {
            foreach (var c in _clips) c.SetInput(mic);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor-time safety net: makes sure every configured AudioSource has
        /// a <see cref="PurrAudioReader"/> component so sample callbacks work
        /// both in Play mode and Edit mode.
        /// </summary>
        private void OnValidate()
        {
            if (_audioSources == null || _audioSources.Count == 0) return;

            foreach (AudioSource src in _audioSources)
            {
                if (src.TryGetComponent<PurrAudioReader>(out var reader))
                    continue;

                // Add the missing component and move to the top of the GameObject.
                reader = src.gameObject.AddComponent<PurrAudioReader>();
                while (ComponentUtility.MoveComponentUp(reader)) { }
            }
        }
#endif
    }
}
