using PurrNet;
using PurrNet.Logging;
using PurrLobby;
using System.Collections;
using UnityEngine;

public class GhostSoundEffects : MonoBehaviour
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
    
    public void PlayScarringAudio()
    {
        if (!m_isOwner)
            return;
        PlayAudio(m_scarringAudioSource, "Scarred");
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
    
    public void PlayDashAudio()
    {
        if (!m_isOwner)
            return;
        PlayAudio(m_dashAudioSource, "Dash");
    }
    
    public void PlayDeathAudio()
    {
        if (!m_isOwner)
            return;
        PlayAudio(m_deathAudioSource, "Death");
    }
    
    public void PlayReviveAudio()
    {
        if (!m_isOwner)
            return;
        PlayAudio(m_reviveAudioSource, "Revive");
    }
    
    public void PlayTransformAudio()
    {
        if (!m_isOwner)
            return;
        PlayAudio(m_transformAudioSource, "Transform");
    }
    
    public void PlayRevivingAudio()
    {
        if (!m_isOwner)
            return;
        PlayAudio(m_revivingAudioSource, "Reviving", true);
    }
    
    public void StopRevivingAudio()
    {
        if (!m_isOwner)
            return;
        StopAudio(m_revivingAudioSource, "Reviving");
    }
    
    public void PlaySabotageAudio()
    {
        if (!m_isOwner)
            return;
        PlayAudio(m_sabotagingAudioSource, "Repair", true);
    }
    
    public void StopSabotageAudio()
    {
        if (!m_isOwner)
            return;
        StopAudio(m_sabotagingAudioSource, "Repair");
    }
    
    public void SetWalkingSpeed(float _speed)
    {
        if (!m_isOwner)
            return;
        //print(_speed);
        if (_speed < 0.1f)
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
