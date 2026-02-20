using PurrNet;
using UnityEngine;

public class PropAnchor : MonoBehaviour
{
    [SerializeField] private GameObject m_propPrefab;

    public void Initialize()
    {
        UnityProxy.Instantiate(m_propPrefab, transform);
    }
}
