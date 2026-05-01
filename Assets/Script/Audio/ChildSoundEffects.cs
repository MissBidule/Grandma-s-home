using System;
using System.Collections;
using PurrNet;
using PurrNet.Logging;
using PurrLobby;
using UnityEngine;

public class ChildSoundEffects : NetworkBehaviour
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

    [ServerRpc]
    public void PlayCustomAudio(AudioClip _clip)
    {
        PlayCustomAudioRPC(_clip);
    }
    
    [ObserversRpc(bufferLast:true)]
    public void PlayCustomAudioRPC(AudioClip _clip)
    {
        // if (!m_isOwner)
        //     return;
        m_gunAudioSource.clip = _clip;
        PlayAudio(m_gunAudioSource, "Gun");
    }
    
    [ServerRpc]
    public void PlayGunAudio()
    {
        PlayGunAudioRPC();
    }
    
    [ObserversRpc(bufferLast:true)]
    public void PlayGunAudioRPC()
    {
        // if (!m_isOwner)
        //     return;
        m_gunAudioSource.clip = m_gunAudioClip;
        PlayAudio(m_gunAudioSource, "Gun");
    }

    [ServerRpc]
    public void PlayHitGhostAudio()
    {
        PlayHitGhostAudioRPC();
    }
    
    [ObserversRpc(bufferLast:true)]
    public void PlayHitGhostAudioRPC()
    {
        // if (!m_isOwner)
        //     return;
        PlayAudio(m_hitGhostAudioSource, "CAC Ghost");
    }
    
    [ServerRpc]
    public void PlayHitAirAudio()
    {
        PlayHitAirAudioRPC();
    }
    
    [ObserversRpc(bufferLast:true)]
    public void PlayHitAirAudioRPC()
    {
        // if (!m_isOwner)
        //     return;
        PlayAudio(m_hitAirAudioSource, "CAC Air");
    }
    
    [ServerRpc]
    public void PlayWeaponSwapAudio()
    {
        PlayWeaponSwapAudioRPC();
    }
    
    [ObserversRpc(bufferLast:true)]
    public void PlayWeaponSwapAudioRPC()
    {
        // if (!m_isOwner)
        //     return;
        PlayAudio(m_weaponSwapAudioSource, "Swap");
    }

    [ServerRpc]
    public void PlayScarredAudio()
    {
        PlayScarredAudioRPC();
    }

    [ObserversRpc(bufferLast:true)]
    public void PlayScarredAudioRPC()
    {
        // if (!m_isOwner)
        //     return;
        PlayAudio(m_scarredAudioSource, "Scarred");
    }

    [ServerRpc]
    public void PlayJumpAudio()
    {
        PlayJumpAudioRPC();
    }

    [ObserversRpc(bufferLast:true)]
    public void PlayJumpAudioRPC()
    {
        // if (!m_isOwner)
        //     return;
        PlayAudio(m_jumpAudioSource, "Jump");
    }

    [ServerRpc]
    public void PlayLandAudio()
    {
        PlayLandAudioRPC();
    }

    [ObserversRpc(bufferLast:true)]
    public void PlayLandAudioRPC()
    {
        // if (!m_isOwner)
        //     return;
        PlayAudio(m_landAudioSource, "Land");
    }

    [ServerRpc]
    public void PlayRepairAudio()
    {
        PlayRepairAudioRPC();
    }
    
    [ObserversRpc(bufferLast:true)]
    public void PlayRepairAudioRPC()
    {
        // if (!m_isOwner)
        //     return;
        PlayAudio(m_repairingAudioSource, "Repair", true);
    }
    
    [ServerRpc]
    public void StopRepairAudio()
    {
        StopRepairAudioRPC();
    }
    
    [ObserversRpc(bufferLast:true)]
    public void StopRepairAudioRPC()
    {
        // if (!m_isOwner)
        //     return;
        StopAudio(m_repairingAudioSource, "Repair");
    }

    [ServerRpc]
    public void SetWalkingSpeed(float _speed)
    {
        SetWalkingSpeedRPC(_speed);
    }
    
    [ObserversRpc(bufferLast:true)]
    public void SetWalkingSpeedRPC(float _speed)
    {
        // if (!m_isOwner)
        //     return;
        //print(_speed);
        if (_speed < 3f)
        {
            m_movementAudioSource.audioSource.Stop();
            return;
        }

        if (!m_movementAudioSource.audioSource.isPlaying)
        {
            m_movementAudioSource.audioSource.Play();
        }
        
        const float maxSpeed = 5;
        m_movementAudioSource.audioSource.volume = (_speed / maxSpeed) * AudioVolumeManager.SFXVolume;
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
                _source.audioSource.loop = true;
                _source.audioSource.Play();
            }
            return;
        }
        _source.audioSource.loop = false;
        _source.audioSource.volume = AudioVolumeManager.SFXVolume;
        _source.audioSource.Play();
    }

    private IEnumerator LoopSource(NetworkAudioSource _source, string _name, float _loopDuration)
    {
        _source.audioSource.loop = true;
        _source.audioSource.Play();
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
        
        _source.audioSource.Stop();
        _source.audioSource.time = 0;
    }
    
}
