using PurrNet;
using PurrNet.Logging;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;
using UnityEngine;

namespace Script.HouseBuilding
{
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
    
    [System.Serializable]
    public class RoomConfig
    {
        public RoomType m_roomType;
        public Transform m_roomAnchor;
        public List<Room> m_roomLayouts;
    }
    
    public class HouseBuilder : NetworkBehaviour
    {
        
        [Header("Rooms References")]
        
        [SerializeField] private List<RoomConfig> m_rooms;
        
        [Header("Props Parameters")]
        [SerializeField, Range(0f, 1f)] private float m_smallPropsPercentage;
        [SerializeField, Range(0f, 1f)] private float m_mediumPropsPercentage;
        
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
         * @The Seeding must be on server only
         */
        private void SeedHouse()
        {
            // Initialize random with a fixed seed so all clients generate the same random values
            int masterSeed = System.DateTime.Now.Millisecond;
            PurrLogger.Log($"Seeding with master seed: {masterSeed}", this);
            
            BuildHouse(masterSeed);
        }

        /*
         * Big function because refactoring caused race issues ; Honestly I was just lazy before
         */
        [ObserversRpc(bufferLast:true)]
        private void BuildHouse(int  _masterSeed)
        {
            BuildHouseInternal(_masterSeed, false);
        }
        
        private void BuildHouseInternal(int _masterSeed, bool _editorMode)
        {
            Random.InitState(_masterSeed);

            int seedIterator = 0;

            foreach (RoomConfig room in m_rooms)
            {
                if (room.m_roomAnchor == null || room.m_roomLayouts == null || room.m_roomLayouts.Count == 0)
                    continue;

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
        
        [SerializeField] private int m_editorSeed = 12345;
        [SerializeField] private bool m_useEditorSeed = true;
        
        private List<GameObject> m_spawnedRooms = new List<GameObject>();
        
        public void EditorBuildHouse()
        {
            ClearEditorHouse();

            int seed = m_useEditorSeed ? m_editorSeed : System.DateTime.Now.Millisecond;;
            BuildHouseInternal(seed, true);
        }

        public void RandomizeSeed()
        {
            m_editorSeed = System.DateTime.Now.Millisecond;
        }
        
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