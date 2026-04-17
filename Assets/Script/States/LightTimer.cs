using System;
using PurrNet;
using UnityEngine;

public class LightTimer : MonoBehaviour
{
    
    [Header("References")]
    [SerializeField] private DayNightSystem m_dayNightSystem;
    [SerializeField] private LightOnSystem m_lightOnSystem;

    private void Awake()
    {
        InstanceHandler.RegisterInstance(this);
        
        if (m_lightOnSystem == null)
            TryGetComponent(out m_lightOnSystem);
        
        if  (m_dayNightSystem == null)
            TryGetComponent(out m_dayNightSystem);
    }

    private void OnDestroy()
    {
        InstanceHandler.UnregisterInstance<LightOnSystem>();
    }

    public void StartLightSystem(float _serverGameTime, int _seed)
    {
        m_dayNightSystem.UpdateSky(_serverGameTime, _seed);
    }
}
