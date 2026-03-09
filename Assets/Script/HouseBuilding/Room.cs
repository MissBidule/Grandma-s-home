using System.Collections.Generic;
using UnityEngine;

namespace Script.HouseBuilding
{
    public class Room : MonoBehaviour
    {
        [Header("Props Infos")]
        [SerializeField] [Tooltip("Anchors used to spawn small props (books, decorations, small furniture, etc.).")] private List<PropAnchor> m_smallPropsAnchors;
        [SerializeField] [Tooltip("Anchors used to spawn medium props (chairs, tables, appliances, etc.).")] private List<PropAnchor> m_mediumPropsAnchors;

        /*
         * @brief Populates the room with props using deterministic random generation.
         * @params _smallPropsPercentage Percentage of small props to spawn.
         * @params _mediumPropsPercentage Percentage of medium props to spawn.
         * @params _randomSeed Seed used to ensure deterministic prop placement.
         * @description The method shuffles the available prop anchors and activates only a
         * percentage of them based on the provided values. Using a seed ensures
         * all clients generate identical prop layouts in multiplayer.
         */
        public void PopulateRoom(float _smallPropsPercentage, float _mediumPropsPercentage, int _randomSeed)
        {
            Random.InitState(_randomSeed);

            // Shuffle the props anchors lists.
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
}