using PurrNet;
using PurrNet.Logging;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;
using UnityEngine;

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
        [SerializeField, Range(0f, 1f)] [Tooltip("Proportion of small props that should be spawned in rooms.")] private float m_smallPropsPercentage;
        [SerializeField, Range(0f, 1f)] [Tooltip("Proportion of medium props that should be spawned in rooms.")] private float m_mediumPropsPercentage;
        
        /*
         * @brief Called when the network object is spawned.
         * @params _asServer Indicates whether this instance is running as the server.
         * @description Only the server is allowed to generate the house. Clients
         * simply receive the result through the network RPC.
         */
        protected override void OnSpawned(bool _asServer)
        {
            base.OnSpawned(_asServer);

            if (!_asServer)
            {
                enabled = false;
                return;
            }

            SeedHouse();
        }

        /*
         * @brief Generates the random seed used for procedural house creation.
         * @description This must only execute on the server so every client
         * receives the same seed and produces identical procedural results.
         */
        private void SeedHouse()
        {
            if (!isServer) // To be super sure
                return;
            
            // Initialize random with a fixed seed so all clients generate the same random values
            int masterSeed = System.DateTime.Now.Millisecond;
            PurrLogger.Log($"Seeding with master seed: {masterSeed}", this);
            
            BuildHouse(masterSeed);
        }

        /*
         * @brief Network RPC responsible for generating the house on clients.
         * @params _masterSeed Deterministic seed used for procedural generation.
         */
        [ObserversRpc(bufferLast:true)]
        private void BuildHouse(int  _masterSeed)
        {
            BuildHouseInternal(_masterSeed, false);
        }
        
        /*
         * @brief Core procedural generation function.
         * @params _masterSeed Seed controlling random generation.
         * @params _editorMode Indicates whether generation is executed inside the editor.
         * @description This function is shared between runtime network generation
         * and editor preview generation.
         */
        private void BuildHouseInternal(int _masterSeed, bool _editorMode)
        {
            Random.InitState(_masterSeed);

            int seedIterator = 0;

            foreach (RoomConfig room in m_rooms)
            {
                if (room.m_roomAnchor == null || room.m_roomLayouts == null || room.m_roomLayouts.Count == 0)
                {
                    PurrLogger.LogWarning($"Error in room definition Type: {room.m_roomType}", this);
                    continue;
                }
                    

                int layoutIndex = Random.Range(0, room.m_roomLayouts.Count);

                Room newRoom;

#if UNITY_EDITOR
                if (_editorMode)
                {
                    GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(
                        room.m_roomLayouts[layoutIndex].gameObject,
                        room.m_roomAnchor
                    );

                    newRoom = go.GetComponent<Room>();
                    m_spawnedRooms.Add(go);
                }
                else
#endif
                {
                    newRoom = UnityProxy.Instantiate(room.m_roomLayouts[layoutIndex], room.m_roomAnchor);
                }

                if (newRoom != null)
                {
                    newRoom.PopulateRoom(
                        m_smallPropsPercentage,
                        m_mediumPropsPercentage,
                        _masterSeed + seedIterator
                    );
                }

                seedIterator++;
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
         */
        public void EditorBuildHouse()
        {
            ClearEditorHouse();

            int seed = m_useEditorSeed ? m_editorSeed : System.DateTime.Now.Millisecond;;
            BuildHouseInternal(seed, true);
        }

        /*
         * @brief Generates the house directly inside the editor.
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