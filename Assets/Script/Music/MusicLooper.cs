using PurrNet;
using PurrNet.Logging;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Script.Music
{
    public enum MusicTrack
    {
        Menu,
        Game,
        Panic
    }
    
    [RequireComponent(typeof(AudioSource))]
    public class MusicLooper : MonoBehaviour
    {
        [Header("Audio Sources")]
        [SerializeField] private AudioSource m_mainAudioSource;
        [SerializeField] private AudioSource m_transitionAudioSource;
        
        [Header("Menu Music")]
        [SerializeField] private AudioClip m_menuMusic;
        [SerializeField] private float m_menuLoopStartTime = 2;
        [SerializeField] private float m_menuLoopEndTime = 30;
        
        [Header("Game Music")]
        [SerializeField] private AudioClip m_gameMusic;
        [SerializeField] private float m_gameTransitionTime = 3;
        [SerializeField] private float m_gameLoopStartTime = 2;
        [SerializeField] private float m_gameLoopEndTime = 30;
        
        [Header("Panic Music")]
        [SerializeField] private AudioClip m_panicMusic;
        [SerializeField] private float m_panicTransitionTime = 3;
        [SerializeField] private float m_panicLoopStartTime = 2;
        [SerializeField] private float m_panicLoopEndTime = 30;
        
        // Volume settings
        private float m_musicVolume = 1; // 0 - 1 the volume of the music

        // Transition Running Parameters
        private MusicTrack m_currentMusicTrack;
        private bool m_inTransition;
        private float m_elapsedTime = 0f;
        
        // Loop Running Parameters
        private int m_loopStartSamples;
        private int m_loopEndSamples;
        private int m_loopLengthSamples;

        private void Awake()
        {
            InstanceHandler.RegisterInstance(this);
            DontDestroyOnLoad(this);
        }

        private void OnDestroy()
        {
            InstanceHandler.UnregisterInstance<MusicLooper>();
        }

        private void Start()
        {
            AudioSource[] audioSources = GetComponents<AudioSource>();

            if (audioSources.Length != 2)
            {
                PurrLogger.LogError("No audio sources found", this);
                return;
            }
            
            m_mainAudioSource = audioSources[0];
            m_transitionAudioSource = audioSources[1];
            
            if (m_menuMusic != null)
                PlayMusic(0);
        }

        public void PlayMusic(MusicTrack _trac)
        {
            switch (_trac)
            {
                case MusicTrack.Menu:
                    m_currentMusicTrack = MusicTrack.Menu;
                    Reset();
                    
                    // Simple set loop parameters and music clip
                    m_mainAudioSource.clip = m_menuMusic;
                    m_loopStartSamples = (int)(m_menuLoopStartTime * m_mainAudioSource.clip.frequency);
                    m_loopEndSamples = (int)(m_menuLoopEndTime * m_mainAudioSource.clip.frequency);
                    m_loopLengthSamples = m_loopEndSamples - m_loopStartSamples;
                    
                    // This is where we start everything
                    m_mainAudioSource.Play();
                    break;
                case MusicTrack.Game:
                    m_currentMusicTrack = MusicTrack.Game;
                    // Setup for transition
                    m_inTransition = true;
                    m_mainAudioSource.volume = m_musicVolume;
                    m_transitionAudioSource.volume = m_musicVolume;
                    m_elapsedTime = 0f;
                    
                    // Get current clip info to transition audio source
                    m_transitionAudioSource.clip = m_mainAudioSource.clip;
                    m_transitionAudioSource.Play();
                    m_transitionAudioSource.timeSamples = m_mainAudioSource.timeSamples;
                    
                    // Setup Main Audio source
                    m_mainAudioSource.clip = m_gameMusic;
                    m_mainAudioSource.Play();
                    
                    // Loop setting
                    m_loopStartSamples = (int)(m_gameLoopStartTime * m_mainAudioSource.clip.frequency);
                    m_loopEndSamples = (int)(m_gameLoopEndTime * m_mainAudioSource.clip.frequency);
                    m_loopLengthSamples = m_loopEndSamples - m_loopStartSamples;
                    break;
                case MusicTrack.Panic:
                    m_currentMusicTrack = MusicTrack.Panic;
                    // Setup for transition
                    m_inTransition = true;
                    
                    // Setup for transition
                    m_inTransition = true;
                    m_mainAudioSource.volume = 0;
                    m_transitionAudioSource.volume = m_musicVolume;
                    m_elapsedTime = 0f;
                    
                    // Get current clip info to transition audio source
                    m_transitionAudioSource.clip = m_mainAudioSource.clip;
                    m_transitionAudioSource.Play();
                    m_transitionAudioSource.timeSamples = m_mainAudioSource.timeSamples;
                    
                    // Setup Main Audio source
                    m_mainAudioSource.clip = m_panicMusic;
                    m_mainAudioSource.Play();
                    
                    // Loop setting
                    m_loopStartSamples = (int)(m_panicLoopStartTime * m_mainAudioSource.clip.frequency);
                    m_loopEndSamples = (int)(m_panicLoopEndTime * m_mainAudioSource.clip.frequency);
                    m_loopLengthSamples = m_loopEndSamples - m_loopStartSamples;
                    break;
                default:
                    PurrLogger.LogError($"Invalid Music Track {typeof(MusicTrack)}", this);
                    break;
            }
        }

        private void Update()
        {
            // Transition stuff
            if (m_inTransition)
                Transition();
            
            // Looping Part
            if (m_mainAudioSource.timeSamples >= m_loopEndSamples)
            {
                m_mainAudioSource.timeSamples -= m_loopLengthSamples;
            }
        }

        private void Transition()
        {
            float transitionTime = m_currentMusicTrack switch
            {
                MusicTrack.Game => m_gameTransitionTime,
                MusicTrack.Panic => m_panicTransitionTime,
                _ => 0
            };

            if (m_elapsedTime >= transitionTime)
            {
                m_mainAudioSource.volume = m_musicVolume;
                m_transitionAudioSource.volume = 0;
                m_transitionAudioSource.Stop();
                m_transitionAudioSource.timeSamples = 0;
                m_inTransition = false;
                m_elapsedTime = 0;
                return;
            }
            // Transitioning
            float transitionProgress = m_elapsedTime / transitionTime;
            m_mainAudioSource.volume = m_musicVolume * transitionProgress;
            m_transitionAudioSource.volume = m_musicVolume - m_musicVolume * transitionProgress;
            
            m_elapsedTime += Time.deltaTime;
        }

        public void Reset()
        {
            m_mainAudioSource.volume = m_musicVolume;
            m_mainAudioSource.timeSamples = 0;
            m_transitionAudioSource.volume = 0;
            m_transitionAudioSource.timeSamples = 0;
        }

        public void StopMusic()
        {
            Reset();
            m_mainAudioSource.Stop();
            m_transitionAudioSource.Stop();
        }
        
        public void SetMusicVolume(float _volume)
        {
            m_musicVolume = _volume;
            if (m_inTransition)
                return;
            m_mainAudioSource.volume = m_musicVolume;
        }
    }
}
