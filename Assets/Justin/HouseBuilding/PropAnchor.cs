using UnityEngine;

public class PropAnchor : MonoBehaviour
{
    [SerializeField] private GameObject m_propPrefab;

    public void Initialize()
    {
        Instantiate(m_propPrefab, transform);
    }
}
