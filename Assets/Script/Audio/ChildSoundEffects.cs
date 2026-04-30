using System;
using System.Collections;
using PurrNet;
using PurrNet.Logging;
using PurrLobby;
using UnityEngine;

public class ChildSoundEffects : MonoBehaviour
{
    
    [Header("Network Audio Sources")]
    [SerializeField] private NetworkAudioSource m_movementAudioSource;
    [SerializeField] private NetworkAudioSource m_gunAudioSource;
    [SerializeField] private NetworkAudioSource m_hitGhostAudioSource;
    [SerializeField] private NetworkAudioSource m_hitAirAudioSource;
    [SerializeField] private NetworkAudioSource m_scarredAudioSource;
    [SerializeField] private NetworkAudioSource m_jumpAudioSource;
    [SerializeField] private NetworkAudioSource m_landAudioSource;
    [SerializeField] private NetworkAudioSource m_repairingAudioSource;
    [SerializeField] private NetworkAudioSource m_weaponSwapAudioSource;
    private AudioClip m_gunAudioClip;

    private bool m_isOwner = false;
    
    public void InitOwner()
    {
        m_isOwner = true;

        m_gunAudioClip = m_gunAudioSource.clip;
        
        if (InstanceHandler.TryGetInstance(out ChildSoundEffects childSoundEffects))
            InstanceHandler.UnregisterInstance<ChildSoundEffects>();
        
        InstanceHandler.RegisterInstance(this);
    }

    private void OnDestroy()
    {
        InstanceHandler.UnregisterInstance<ChildSoundEffects>();
    }
    
    // Movement Will need a bigger script don't touch

    public void PlayCustomAudio(AudioClip _clip)
    {
        if (!m_isOwner)
            return;
        m_gunAudioSource.clip = _clip;
        PlayAudio(m_gunAudioSource, "Gun");
    }
    
    public void PlayGunAudio()
    {
        if (!m_isOwner)
            return;
        m_gunAudioSource.clip = m_gunAudioClip;
        PlayAudio(m_gunAudioSource, "Gun");
    }

    public void PlayHitGhostAudio()
    {
        if (!m_isOwner)
            return;
        PlayAudio(m_hitGhostAudioSource, "CAC Ghost");
    }
    
    public void PlayHitAirAudio()
    {
        if (!m_isOwner)
            return;
        PlayAudio(m_hitAirAudioSource, "CAC Air");
    }
    
    public void PlayWeaponSwapAudio()
    {
        if (!m_isOwner)
            return;
        PlayAudio(m_weaponSwapAudioSource, "Swap");
    }

    public void PlayScarredAudio()
    {
        if (!m_isOwner)
            return;
        PlayAudio(m_scarredAudioSource, "Scarred");
    }

    public void PlayJumpAudio()
    {
        if (!m_isOwner)
            return;
        PlayAudio(m_jumpAudioSource, "Jump");
    }

    public void PlayLandAudio()
    {
        if (!m_isOwner)
            return;
        PlayAudio(m_landAudioSource, "Land");
    }

    public void PlayRepairAudio()
    {
        if (!m_isOwner)
            return;
        PlayAudio(m_repairingAudioSource, "Repair", true);
    }
    
    public void StopRepairAudio()
    {
        if (!m_isOwner)
            return;
        StopAudio(m_repairingAudioSource, "Repair");
    }

    public void SetWalkingSpeed(float _speed)
    {
        if (!m_isOwner)
            return;
        //print(_speed);
        if (_speed < 3f)
        {
            m_movementAudioSource.Stop();
            return;
        }

        if (!m_movementAudioSource.isPlaying)
        {
            m_movementAudioSource.Play();
        }
        
        const float maxSpeed = 5;
        m_movementAudioSource.volume = (_speed / maxSpeed) * AudioVolumeManager.SFXVolume;
    }

    private void PlayAudio(NetworkAudioSource _source, string _name, bool _loop = false, float _loopDuration = 0)
    {
        if (_source == null)
        {
            PurrLogger.LogError($"{name} Audio Source is null");
            return;
        }

        if (_source.clip == null)
        {
            PurrLogger.LogError($"{name} Audio Clip is null");
            return;
        }

        if (_loop)
        {
            if (_loopDuration > 0)
                StartCoroutine(LoopSource(_source, _name, _loopDuration));
            else
            {
                _source.loop = true;
                _source.Play();
            }
            return;
        }
        _source.loop = false;
        _source.volume = AudioVolumeManager.SFXVolume;
        _source.Play();
    }

    private IEnumerator LoopSource(NetworkAudioSource _source, string _name, float _loopDuration)
    {
        _source.loop = true;
        _source.Play();
        yield return new WaitForSeconds(_loopDuration);
        StopAudio(_source, _name);
    }

    private void StopAudio(NetworkAudioSource _source, string _name)
    {
        if (_source == null)
        {
            PurrLogger.LogError($"{name} Audio Source is null");
            return;
        }
        
        if (_source.clip == null)
        {
            PurrLogger.LogError($"{name} Audio Clip is null");
            return;
        }
        
        _source.Stop();
        _source.time = 0;
    }
    
}
