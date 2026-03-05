using System.Collections.Generic;
using UnityEngine;

public class Room : MonoBehaviour
{
    [Header("Props Infos")]
    [SerializeField] private List<PropAnchor> m_smallPropsAnchors;
    [SerializeField] private List<PropAnchor> m_mediumPropsAnchors;

    public void PopulateRoom(float _smallPropsPercentage, float _mediumPropsPercentage, int _randomSeed)
    {
        Random.InitState(_randomSeed);
        
        // Shuffle the lists

        for (var i = m_smallPropsAnchors.Count - 1; i > 0; i--)
        {
            var randomIndex = Random.Range(0, m_smallPropsAnchors.Count);
            (m_smallPropsAnchors[i], m_smallPropsAnchors[randomIndex]) = (m_smallPropsAnchors[randomIndex], m_smallPropsAnchors[i]);
        }
        
        for (var i = m_mediumPropsAnchors.Count - 1; i > 0; i--)
        {
            var randomIndex = Random.Range(0, m_mediumPropsAnchors.Count);
            (m_mediumPropsAnchors[i], m_mediumPropsAnchors[randomIndex]) = (m_mediumPropsAnchors[randomIndex], m_mediumPropsAnchors[i]);
        }
        
        // Initialize the given proportion of the room props

        for (var index = 0; index < m_smallPropsAnchors.Count * _smallPropsPercentage; index++)
        {
            m_smallPropsAnchors[index].Initialize();
        }

        for (var index = 0; index < m_mediumPropsAnchors.Count * _mediumPropsPercentage; index++)
        {
            m_mediumPropsAnchors[index].Initialize();
        }
    }
}
