using System;
using UnityEngine;
using UnityEngine.Audio;

namespace PurrLobby
{
    public static class AudioVolumeManager
    {
        public static float SFXVolume {get; private set;} = .5f;

        public static event Action<float> OnMusicVolumeChanged;

        private static AudioMixer s_mixer;

        /*
         * @brief Lazily loads the GameMixer asset from Resources on first access.
         * @return The cached AudioMixer instance, or null if the asset is missing
         */
        public static AudioMixer Mixer
        {
            get
            {
                if (s_mixer == null) s_mixer = Resources.Load<AudioMixer>("GameMixer");
                return s_mixer;
            }
        }

        /*
         * @brief Applies persisted volume preferences once the first scene has finished loading.
         */
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnGameStart() => ApplyFromPrefs();

        /*
         * @brief Reads master, music and SFX volumes from PlayerPrefs and pushes them to the mixer
         * and the AudioListener. Defaults to 1.0 for any missing key.
         */
        public static void ApplyFromPrefs()
        {
            SetMaster(PlayerPrefs.GetFloat("Settings_VolMaster",.5f));
            SetMusic(PlayerPrefs.GetFloat("Settings_VolMusic",.5f));
            SetSFX(PlayerPrefs.GetFloat("Settings_VolSFX",.5f));
        }

        /*
         * @brief Sets the global master volume via the AudioListener so that even AudioSources
         * not routed through the mixer are affected.
         * @param _v: linear volume in [0, 1]
         */
        public static void SetMaster(float _v) => AudioListener.volume = _v;

        /*
         * @brief Updates the mixer's exposed Music group volume and raises the legacy
         *        OnMusicVolumeChanged event for any remaining listeners.
         * @param _v: linear volume in [0, 1]
         */
        public static void SetMusic(float _v)
        {
            if (Mixer != null) Mixer.SetFloat("MusicVolume", LinearToDb(_v));
            OnMusicVolumeChanged?.Invoke(_v);
        }

        /*
         * @brief Updates the mixer's exposed SFX group volume
         * @param _v: linear volume in [0, 1]
         */
        public static void SetSFX(float _v)
        {
            SFXVolume = _v;
            if (Mixer != null) Mixer.SetFloat("SFXVolume", LinearToDb(_v));
        }

        /*
         * @brief Converts a linear volume in [0, 1] to a decibel attenuation suitable for an AudioMixer exposed parameter.
         * @param _v: linear volume in [0, 1]
         * @return decibel value in [-80, 0]
         */
        private static float LinearToDb(float _v) => _v <= 0.0001f ? -80f : Mathf.Log10(_v) * 20f;
    }
}
