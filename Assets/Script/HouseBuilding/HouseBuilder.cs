using PurrNet;
using PurrNet.Logging;
using System;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Script.HouseBuilding
{
    /*
    // * @brief Enumeration listing every supported room type in the house.
    // * @description Used mainly for debugging and validation of room configurations
    // * in the inspector. It helps to identify which room configuration failed
    // * during generation.
    // */ 
    public enum RoomType
    {
        LivingRoom,
        LivingRoom2,
        DiningRoom,
        Solarium,
        ChildrenBedroom,
        GrandmotherBedroom,
        Bathroom,
        DressingRoom,
        Pantry,
        Toilet1,
        Toilet2,
        LaundryRoom,
        Closet1,
        Closet2,
        Garage,
        ArtWorkshop,
        MusicWorkshop,
        InventionsWorkshop,
        Library,
        Office,
        GameRoom
    }
    
    /*
     * @brief Configuration container describing how a room should spawn.
     * @description Each configuration defines the room type, the transform
     * anchor where the room should be instantiated, and a list of possible
     * room layouts that can be randomly selected.
     */
    [System.Serializable]
    public class RoomConfig
    {
        public RoomType m_roomType;
        public Transform m_roomAnchor;
        public List<Room> m_roomLayouts;
    }
    
    /*
     * @brief Responsible for generating the house structure and populating rooms.
     * @description The generation is executed only on the server to ensure
     * deterministic procedural generation across all clients. The resulting
     * layout is synchronized using an Observers RPC.
     */
    public class HouseBuilder : NetworkBehaviour
    {
        
        [Header("Rooms References")]
        
        /*
         * @brief List of all rooms used to build the house.
         * @description Each entry defines the spawn anchor and the possible
         * layouts that can be randomly chosen during generation.
         */
        [SerializeField] private List<RoomConfig> m_rooms;
        
        [Header("Props Parameters")]
        [SerializeField, Range(0f, 1f)] [Tooltip("Proportion of small props that should be spawned in rooms.")] public float m_smallPropsPercentage;
        [SerializeField, Range(0f, 1f)] [Tooltip("Proportion of medium props that should be spawned in rooms.")] public float m_mediumPropsPercentage;
        public SyncVar<int> m_masterSeed; 
        
        /*
         * @brief Called when the network object is spawned.
         * @params _asServer Indicates whether this instance is running as the server.
         * @description Only the server is allowed to generate the house. Clients
         * simply receive the result through the network RPC.
         */
        private void Awake()
        {
            InstanceHandler.RegisterInstance(this);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            
            InstanceHandler.UnregisterInstance<HouseBuilder>();
        }

        protected override void OnSpawned()
        {
            base.OnSpawned();
            BuildHouseNetwork();
        }

        /*
         * @brief Generates the random seed used for procedural house creation.
         * @description This must only execute on the server so every client
         * receives the same seed and produces identical procedural results.
         */
        private void BuildHouseNetwork()
        {
            if (!isServer)
                return;
            
            m_masterSeed = new SyncVar<int>(DateTime.Now.Millisecond);
            PurrLogger.Log($"Seeding house with master seed: {m_masterSeed}", this);

            BuildHouseNetwork(m_masterSeed);
        }

        /*
         * @brief Network RPC responsible for generating the house on all clients.
         * @params _masterSeed Deterministic seed used for procedural generation.
         * @description This RPC is called by the server and executed on all observers
         * to build the house structure synchronously.
         */
        //[ObserversRpc(bufferLast:true)]
        //private void BuildHouse(int _masterSeed)
        //{
            //BuildHouseNetwork(_masterSeed);
        //}

        /*
         * @brief Network implementation of house generation.
         * @params _masterSeed Seed controlling random generation for all clients.
         * @description Creates rooms on all clients using the same seed to ensure
         * identical layouts. Props are populated separately via server-side RPC.
         */
        private void BuildHouseNetwork(int _masterSeed)
        {
            Random.InitState(_masterSeed);
            PurrLogger.Log($"Building house with master seed: {_masterSeed}", this);

            foreach (RoomConfig room in m_rooms)
            {
                if (room.m_roomAnchor == null || room.m_roomLayouts == null || room.m_roomLayouts.Count == 0)
                {
                    PurrLogger.LogWarning($"Error in room definition Type: {room.m_roomType}", this);
                    continue;
                }

                int layoutIndex = Random.Range(0, room.m_roomLayouts.Count);
                Room newRoom = UnityProxy.Instantiate(room.m_roomLayouts[layoutIndex], room.m_roomAnchor);
            }
        }
        
        /*
         * ================================================================================
         * =========================== Editor Stuff From there ============================
         * ================================================================================
         */
        #if UNITY_EDITOR
        
        [Header("Editor Builder")]
        
        [SerializeField] [Tooltip("Seed used for editor house generation.")] private int m_editorSeed = 12345;
        [SerializeField] [Tooltip("Determines whether the editor should use the fixed seed.")] private bool m_useEditorSeed = true;
        
        /*
         * @brief Stores references to rooms spawned in editor mode.
         */
        private List<GameObject> m_spawnedRooms = new List<GameObject>();
        
        /*
         * @brief Generates the house directly inside the editor.
         * @description Uses PrefabUtility to instantiate room prefabs in editor mode
         * and populates them with props immediately for preview.
         */
        public void EditorBuildHouse()
        {
            ClearEditorHouse();

            int seed = m_useEditorSeed ? m_editorSeed : System.DateTime.Now.Millisecond;
            BuildHouseEditor(seed);
        }

        /*
         * @brief Editor-specific house generation implementation.
         * @params _seed Seed used for deterministic editor preview.
         * @description Creates rooms and props using editor-only instantiation methods
         * (PrefabUtility) for proper scene preservation.
         */
        private void BuildHouseEditor(int _seed)
        {
            Random.InitState(_seed);

            int seedIterator = 0;

            foreach (RoomConfig room in m_rooms)
            {
                if (room.m_roomAnchor == null || room.m_roomLayouts == null || room.m_roomLayouts.Count == 0)
                {
                    PurrLogger.LogWarning($"Error in room definition Type: {room.m_roomType}", this);
                    continue;
                }

                int layoutIndex = Random.Range(0, room.m_roomLayouts.Count);

                GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(
                    room.m_roomLayouts[layoutIndex].gameObject,
                    room.m_roomAnchor
                );

                Room newRoom = go.GetComponent<Room>();
                m_spawnedRooms.Add(go);

                if (newRoom != null)
                {
                    newRoom.PopulateRoom(
                        m_smallPropsPercentage,
                        m_mediumPropsPercentage,
                        _seed + seedIterator
                    );
                }

                seedIterator++;
            }
        }

        /*
         * @brief Randomizes the editor seed for testing different house layouts.
         */
        public void RandomizeSeed()
        {
            m_editorSeed = System.DateTime.Now.Millisecond;
        }
        
        /*
         * @brief Removes all rooms spawned during editor preview.
         */
        public void ClearEditorHouse()
        {
            foreach (var room in m_spawnedRooms)
            {
                if (room != null)
                    DestroyImmediate(room);
            }

            m_spawnedRooms.Clear();
        }
        #endif

    }
}