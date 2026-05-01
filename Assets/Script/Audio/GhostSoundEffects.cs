using PurrNet;
using PurrNet.Logging;
using PurrLobby;
using System.Collections;
using UnityEngine;

public class GhostSoundEffects : NetworkBehaviour
{
    [Header("Network Audio Sources")]
    [SerializeField] private NetworkAudioSource m_movementAudioSource;
    [SerializeField] private NetworkAudioSource m_scarringAudioSource;
    [SerializeField] private NetworkAudioSource m_jumpAudioSource;
    [SerializeField] private NetworkAudioSource m_landAudioSource;
    [SerializeField] private NetworkAudioSource m_dashAudioSource;
    [SerializeField] private NetworkAudioSource m_sabotagingAudioSource;
    [SerializeField] private NetworkAudioSource m_deathAudioSource;
    [SerializeField] private NetworkAudioSource m_reviveAudioSource;
    [SerializeField] private NetworkAudioSource m_revivingAudioSource;
    [SerializeField] private NetworkAudioSource m_transformAudioSource;
    private bool m_isOwner = false;
    
    public void InitOwner()
    {
        m_isOwner = true;
        
        if (InstanceHandler.TryGetInstance(out ChildSoundEffects childSoundEffects))
            InstanceHandler.UnregisterInstance<ChildSoundEffects>();
        
        InstanceHandler.RegisterInstance(this);
    }

    private void OnDestroy()
    {
        InstanceHandler.UnregisterInstance<ChildSoundEffects>();
    }
    
    [ServerRpc]
    public void PlayScarringAudio()
    {
        PlayScarringAudioRPC();
    }
    
    [ObserversRpc(bufferLast:true)]
    public void PlayScarringAudioRPC()
    {
        // if (!m_isOwner)
        //     return;
        PlayAudio(m_scarringAudioSource, "Scarred");
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
    public void PlayDashAudio()
    {
        PlayDashAudioRPC();
    }
    
    [ObserversRpc(bufferLast:true)]
    public void PlayDashAudioRPC()
    {
        // if (!m_isOwner)
        //     return;
        PlayAudio(m_dashAudioSource, "Dash");
    }
    
    [ServerRpc]
    public void PlayDeathAudio()
    {
        PlayDeathAudioRPC();
    }
    
    [ObserversRpc(bufferLast:true)]
    public void PlayDeathAudioRPC()
    {
        // if (!m_isOwner)
        //     return;
        PlayAudio(m_deathAudioSource, "Death");
    }
    
    [ServerRpc]
    public void PlayReviveAudio()
    {
        PlayReviveAudioRPC();
    }
    
    [ObserversRpc(bufferLast:true)]
    public void PlayReviveAudioRPC()
    {
        // if (!m_isOwner)
        //     return;
        PlayAudio(m_reviveAudioSource, "Revive");
    }
    
    [ServerRpc]
    public void PlayTransformAudio()
    {
        PlayTransformAudioRPC();
    }
    
    [ObserversRpc(bufferLast:true)]
    public void PlayTransformAudioRPC()
    {
        // if (!m_isOwner)
        //     return;
        PlayAudio(m_transformAudioSource, "Transform");
    }
    
    [ServerRpc]
    public void PlayRevivingAudio()
    {
        PlayRevivingAudioRPC();
    }
    
    [ObserversRpc(bufferLast:true)]
    public void PlayRevivingAudioRPC()
    {
        // if (!m_isOwner)
        //     return;
        PlayAudio(m_revivingAudioSource, "Reviving", true);
    }
    
    [ServerRpc]
    public void StopRevivingAudio()
    {
        StopRevivingAudioRPC();
    }
    
    [ObserversRpc(bufferLast:true)]
    public void StopRevivingAudioRPC()
    {
        // if (!m_isOwner)
        //     return;
        StopAudio(m_revivingAudioSource, "Reviving");
    }
    
    [ServerRpc]
    public void PlaySabotageAudio()
    {
        PlaySabotageAudioRPC();
    }
    
    [ObserversRpc(bufferLast:true)]
    public void PlaySabotageAudioRPC()
    {
        // if (!m_isOwner)
        //     return;
        PlayAudio(m_sabotagingAudioSource, "Repair", true);
    }
    
    [ServerRpc]
    public void StopSabotageAudio()
    {
        StopSabotageAudioRPC();
    }
    
    [ObserversRpc(bufferLast:true)]
    public void StopSabotageAudioRPC()
    {
        // if (!m_isOwner)
        //     return;
        StopAudio(m_sabotagingAudioSource, "Repair");
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
        if (_speed < 0.1f)
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
