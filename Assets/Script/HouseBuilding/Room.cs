using PurrNet;
using PurrNet.Logging;
using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Script.HouseBuilding
{
    public struct LinkingPacket
    {
        public GameObject m_trapdoorEntry;
        public RoomType m_roomType;
        public GameObject m_trapdoorExit;
        public RoomType m_trapdoorExitRoomType;
    }
    
    public class Room : NetworkBehaviour
    {
        [Header("Networked Objects")]
        [SerializeField] [Tooltip("Sabotage Object.")] private SabotageObject m_sabotageObject;
        [SerializeField] [Tooltip("(The one you go in)")] private GameObject m_trapdoorEntry;
        [SerializeField] [Tooltip("(The one you exit from)")] private GameObject m_trapdoorExit; // TODO change to trapdoor type
        [SerializeField] [Tooltip("(Where you go after entering the trapdoor in this room.)")] private RoomType m_trapdoorExitRoomType;
        
        [Header("Props Infos")]
        [SerializeField] [Tooltip("Anchors used to spawn small props (books, decorations, small furniture, etc.).")] private List<PropAnchor> m_smallPropsAnchors;
        [SerializeField] [Tooltip("Anchors used to spawn medium props (chairs, tables, appliances, etc.).")] private List<PropAnchor> m_mediumPropsAnchors;
        
        private RoomType m_roomType;

        private void Awake()
        {
            // Object Validation
            if (m_sabotageObject == null)
                PurrLogger.LogError($"{m_roomType} Sabotage Object is null", this);
            if (m_trapdoorEntry == null)
                PurrLogger.LogError($"{m_roomType} Trapdoor Entry is null", this);
            if (m_trapdoorExit == null)
                PurrLogger.LogError($"{m_roomType} Trapdoor Exit is null", this);
            if (m_smallPropsAnchors == null)
                PurrLogger.LogError($"{m_roomType} Small Props Anchors is null", this);
            if (m_mediumPropsAnchors == null)
                PurrLogger.LogError($"{m_roomType} Medium Props Anchors is null", this);
        }
        
        public SabotageObject GetSabotageObject()
        {
            return m_sabotageObject;
        }

        public LinkingPacket GetTrapLinkingPacket()
        {
            LinkingPacket packet;
            packet.m_trapdoorEntry = m_trapdoorEntry;
            packet.m_roomType = m_roomType;
            packet.m_trapdoorExit = m_trapdoorExit;
            packet.m_trapdoorExitRoomType = m_trapdoorExitRoomType;
            return packet;
        }

        public void SetRoomType(RoomType _roomType)
        {
            m_roomType = _roomType;
        }

        protected override void OnSpawned()
        {
            base.OnSpawned();
            
            if (!isServer)
                return;
            
            if (!InstanceHandler.TryGetInstance(out HouseBuilder houseBuilder))
                return;
            
            PopulateRoomNetwork(houseBuilder.m_smallPropsPercentage, houseBuilder.m_mediumPropsPercentage,
                houseBuilder.m_masterSeed);
        }
        
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
        }

        private void PopulateRoomNetwork(float _smallPropsPercentage, float _mediumPropsPercentage, int _randomSeed)
        {
            Random.InitState(_randomSeed);
            
            PurrLogger.Log($"Populating Room Network {name}", this);
            
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
                if (m_smallPropsAnchors[index] == null)
                {
                    PurrLogger.LogError($"PropAnchor Network initialization failed (Anchor {index} malformed)", this);
                    continue;
                }
                    
                m_smallPropsAnchors[index].NetworkInitialize(m_smallPropsAnchors[index].transform);
            }

            for (int index = 0; index < m_mediumPropsAnchors.Count * _mediumPropsPercentage; index++)
            {
                if (m_mediumPropsAnchors[index] == null)
                {
                    PurrLogger.LogError($"PropAnchor Network initialization failed (Anchor {index} malformed)", this);
                    continue;
                }
                m_mediumPropsAnchors[index].NetworkInitialize(m_mediumPropsAnchors[index].transform);
            }
        }
    }
}