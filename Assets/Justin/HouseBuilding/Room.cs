using System.Collections.Generic;
using UnityEngine;

public class Room : MonoBehaviour
{
    [Header("Props Infos")]
    [SerializeField] private List<PropAnchor> m_smallPropsAnchors;
    [SerializeField] private List<PropAnchor> m_mediumPropsAnchors;

    public void PopulateRoom(float _smallPropsPercentage, float _mediumPropsPercentage)
    {
        foreach (PropAnchor propAnchor in m_smallPropsAnchors)
        {
            if (Random.Range(0f, 1f) < _smallPropsPercentage)
                propAnchor.Initialize();
        }
        
        foreach (PropAnchor propAnchor in m_mediumPropsAnchors)
        {
            if (Random.Range(0f, 1f) < _mediumPropsPercentage)
                propAnchor.Initialize();
        }
    }
}
