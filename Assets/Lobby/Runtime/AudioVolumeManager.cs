using System;
using UnityEngine;

namespace PurrLobby
{
    public static class AudioVolumeManager
    {
        public static float SFXVolume {get; private set;} = 1f;

        public static event Action<float> OnMusicVolumeChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnGameStart() => ApplyFromPrefs();

        public static void ApplyFromPrefs()
        {
            SetMaster(PlayerPrefs.GetFloat("Settings_VolMaster",0.5f));
            SetMusic(PlayerPrefs.GetFloat("Settings_VolMusic",0.5f));
            SetSFX(PlayerPrefs.GetFloat("Settings_VolSFX",1f));
        }

        public static void SetMaster(float v) => AudioListener.volume = v;
        public static void SetMusic(float v) => OnMusicVolumeChanged?.Invoke(v);
        public static void SetSFX(float v)=> SFXVolume = v;
    }
}
