using PurrNet;
using System.Collections.Generic;
using UnityEngine;

namespace Script.HouseBuilding
{
    public class Room : NetworkBehaviour
    {
        [Header("Networked Objects")]
        [SerializeField] [Tooltip("Prefab of the Sabotage Object.")] private SabotageObject m_sabotagePrefab;
        [SerializeField] [Tooltip("Anchor of the Sabotage Object.")] private Transform m_sabotageAnchor;
        [SerializeField] [Tooltip("Prefab of the trapdoor Entry.")] private GameObject m_trapdoorPrefab;
        [SerializeField] [Tooltip("Anchor of the trapdoor Entry.")] private Transform m_trapdoorAnchor;
        [SerializeField] [Tooltip("The room type of the exit of this room trapdoor.")] private RoomType m_trapdoorExitRoomType;
        [SerializeField] [Tooltip("Prefab of the trapdoor exit.")] private GameObject m_trapdoorExitPrefab;
        [SerializeField] [Tooltip("Anchor of the trapdoor exit.")] private Transform m_trapdoorExitAnchor;
        
        [Header("Props Infos")]
        [SerializeField] [Tooltip("Anchors used to spawn small props (books, decorations, small furniture, etc.).")] private List<PropAnchor> m_smallPropsAnchors;
        [SerializeField] [Tooltip("Anchors used to spawn medium props (chairs, tables, appliances, etc.).")] private List<PropAnchor> m_mediumPropsAnchors;

        // Variables for linking.
        private SabotageObject m_sabotageObject;
        private GameObject m_trapdoorEntry;
        private GameObject m_trapdoorExit;
        
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
            for (int i = m_smallPropsAnchors.Count - 1; i > 0; i--)
            {
                int randomIndex = Random.Range(0, m_smallPropsAnchors.Count);
                (m_smallPropsAnchors[i], m_smallPropsAnchors[randomIndex]) = (m_smallPropsAnchors[randomIndex], m_smallPropsAnchors[i]);
            }

            for (int i = m_mediumPropsAnchors.Count - 1; i > 0; i--)
            {
                int randomIndex = Random.Range(0, m_mediumPropsAnchors.Count);
                (m_mediumPropsAnchors[i], m_mediumPropsAnchors[randomIndex]) = (m_mediumPropsAnchors[randomIndex], m_mediumPropsAnchors[i]);
            }

            // Initialize the given proportion of the room props
            for (int index = 0; index < m_smallPropsAnchors.Count * _smallPropsPercentage; index++)
            {
                m_smallPropsAnchors[index].Initialize();
            }

            for (int index = 0; index < m_mediumPropsAnchors.Count * _mediumPropsPercentage; index++)
            {
                m_mediumPropsAnchors[index].Initialize();
            }
            
            // Spawn the trapdoor and sabotage object
            if (m_sabotagePrefab != null)
            {
                m_sabotageObject = Instantiate(m_sabotagePrefab, m_sabotageAnchor);
            }

            if (m_trapdoorPrefab != null)
            {
                m_trapdoorEntry = Instantiate(m_trapdoorPrefab, m_trapdoorAnchor);
            }

            if (m_trapdoorExitPrefab != null)
            {
                m_trapdoorExit = Instantiate(m_trapdoorExitPrefab, m_trapdoorExitAnchor);
            }
        }

        public void PopulateRoomNetwork(float _smallPropsPercentage, float _mediumPropsPercentage, int _randomSeed)
        {
            Random.InitState(_randomSeed);
            
            // Shuffle the props anchors lists.
            for (int i = m_smallPropsAnchors.Count - 1; i > 0; i--)
            {
                int randomIndex = Random.Range(0, m_smallPropsAnchors.Count);
                (m_smallPropsAnchors[i], m_smallPropsAnchors[randomIndex]) = (m_smallPropsAnchors[randomIndex], m_smallPropsAnchors[i]);
            }

            for (int i = m_mediumPropsAnchors.Count - 1; i > 0; i--)
            {
                int randomIndex = Random.Range(0, m_mediumPropsAnchors.Count);
                (m_mediumPropsAnchors[i], m_mediumPropsAnchors[randomIndex]) = (m_mediumPropsAnchors[randomIndex], m_mediumPropsAnchors[i]);
            }

            // Initialize the given proportion of the room props
            for (int index = 0; index < m_smallPropsAnchors.Count * _smallPropsPercentage; index++)
            {
                m_smallPropsAnchors[index].NetworkInitialize();
            }

            for (int index = 0; index < m_mediumPropsAnchors.Count * _mediumPropsPercentage; index++)
            {
                m_mediumPropsAnchors[index].NetworkInitialize();
            }
            
            // Spawn the trapdoor and sabotage object
            if (m_sabotagePrefab != null)
            {
                m_sabotageObject = Instantiate(m_sabotagePrefab, m_sabotageAnchor);
            }

            if (m_trapdoorPrefab != null)
            {
                m_trapdoorEntry = Instantiate(m_trapdoorPrefab, m_trapdoorAnchor);
            }

            if (m_trapdoorExitPrefab != null)
            {
                m_trapdoorExit = Instantiate(m_trapdoorExitPrefab, m_trapdoorExitAnchor);
            }
        }
        
        // TODO make the linking
    }
}