/*
 * @brief  Contains class declaration for StartingDoor
 * @details DOOR
 */
using System.Collections;
using PurrNet;
using UnityEngine;

public class StartingDoor : NetworkBehaviour
{
    [Header("Door Pivots")]
    [SerializeField] [Tooltip("(rotates +Y)")] private Transform m_pivot1;
    [SerializeField] [Tooltip("(rotates -Y)")] private Transform m_pivot2;

    [Header("Animation")]
    [SerializeField] [Tooltip("LES ANGLES")] private float m_openAngle = 105f;
    [SerializeField] [Tooltip("c'est long la non ?")] private float m_openDuration = 3f;

    [Header("Invisible Wall")]
    [SerializeField] private GameObject m_invisibleWall;
    
    [Header("SFX")]
    [SerializeField] private NetworkAudioSource m_soundEffects;

    [ObserversRpc(runLocally: true, requireServer: true)]
    public void OpenDoors()
    {
        m_invisibleWall.SetActive(false);
        if (m_soundEffects.clip != null)
            m_soundEffects.Play();
        StartCoroutine(AnimateDoor(m_pivot1, m_openAngle));
        StartCoroutine(AnimateDoor(m_pivot2, -m_openAngle));
    }

    private IEnumerator AnimateDoor(Transform _pivot, float _targetAngle)
    {
        float elapsed = 0f;
        Quaternion startRot = _pivot.localRotation;
        Quaternion endRot = startRot * Quaternion.Euler(0f, _targetAngle, 0f);

        while (elapsed < m_openDuration)
        {
            elapsed += Time.deltaTime;
            _pivot.localRotation = Quaternion.Lerp(startRot, endRot, elapsed / m_openDuration);
            yield return null;
        }

        _pivot.localRotation = endRot;
    }
}
