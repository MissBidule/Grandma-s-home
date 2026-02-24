using PurrNet;
using PurrNet.Logging;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public class HouseBuilder : NetworkBehaviour
{
    private enum RoomType
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
    
    
    [Header("Rooms References")]
    
    [SerializeField] private Transform m_livingRoomAnchor;
    [SerializeField] private List<Room> m_livingRoomLayouts;
    
    [SerializeField] private Transform m_livingRoom2Anchor;
    [SerializeField] private List<Room> m_livingRoom2Layouts;

    [SerializeField] private Transform m_diningRoomAnchor;
    [SerializeField] private List<Room> m_diningRoomLayouts;
    
    [SerializeField] private Transform m_solariumAnchor;
    [SerializeField] private List<Room> m_solariumLayouts;
    
    [SerializeField] private Transform m_childrenBedroomAnchor;
    [SerializeField] private List<Room> m_childrenBedroomLayouts;
    
    [SerializeField] private Transform m_grandmotherBedroomAnchor;
    [SerializeField] private List<Room> m_grandmotherBedroomLayouts;
    
    [SerializeField] private Transform m_bathroomAnchor;
    [SerializeField] private List<Room> m_bathroomLayouts;
    
    [SerializeField] private Transform m_dressingRoomAnchor;
    [SerializeField] private List<Room> m_dressingRoomLayouts;
    
    [SerializeField] private Transform m_pantryAnchor;
    [SerializeField] private List<Room> m_pantryLayouts;
    
    [SerializeField] private Transform m_toilet1Anchor;
    [SerializeField] private List<Room> m_toilet1Layouts;
    
    [SerializeField] private Transform m_toilet2Anchor;
    [SerializeField] private List<Room> m_toilet2Layouts;
    
    [SerializeField] private Transform m_laundryRoomAnchor;
    [SerializeField] private List<Room> m_laundryRoomLayouts;
    
    [SerializeField] private Transform m_closet1Anchor;
    [SerializeField] private List<Room> m_closet1Layouts;
    
    [SerializeField] private Transform m_closet2Anchor;
    [SerializeField] private List<Room> m_closet2Layouts;
    
    [SerializeField] private Transform m_garageAnchor;
    [SerializeField] private List<Room> m_garageLayouts;
    
    [SerializeField] private Transform m_artWorkshopAnchor;
    [SerializeField] private List<Room> m_artWorkshopLayouts;
    
    [SerializeField] private Transform m_musicWorkshopAnchor;
    [SerializeField] private List<Room> m_musicWorkshopLayouts;
    
    [SerializeField] private Transform m_inventionsWorkshopAnchor;
    [SerializeField] private List<Room> m_inventionsWorkshopLayouts;
    
    [SerializeField] private Transform m_libraryAnchor;
    [SerializeField] private List<Room> m_libraryLayouts;
    
    [SerializeField] private Transform m_officeAnchor;
    [SerializeField] private List<Room> m_officeLayouts;
    
    [SerializeField] private Transform m_gameRoomAnchor;
    [SerializeField] private List<Room> m_gameRoomLayouts;
    
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
        

        RoomsValidation();

        SeedHouse();
    }

    private void RoomsValidation()
    {
        if (m_livingRoomAnchor == null)
            PurrLogger.LogError("Living Room Anchor not set", this);
        
        if (m_livingRoomLayouts == null || m_livingRoomLayouts.Count == 0)
            PurrLogger.LogError("Living Room Layouts not set or empty", this);

        if (m_livingRoom2Anchor == null)
            PurrLogger.LogError("Living Room 2 Anchor not set", this);
        
        if (m_livingRoom2Layouts == null || m_livingRoom2Layouts.Count == 0)
            PurrLogger.LogError("Living Room 2 Layouts not set or empty", this);
        
        if (m_diningRoomAnchor == null)
            PurrLogger.LogError("Dining Room Anchor not set", this);
        
        if (m_diningRoomLayouts == null || m_diningRoomLayouts.Count == 0)
            PurrLogger.LogError("Dining Room Layouts not set or empty", this);
        
        if (m_solariumAnchor == null)
            PurrLogger.LogError("Solarium Anchor not set", this);
        
        if (m_solariumLayouts == null || m_solariumLayouts.Count == 0)
            PurrLogger.LogError("Solarium Layouts not set or empty", this);
        
        if (m_childrenBedroomAnchor == null)
            PurrLogger.LogError("Children Bedroom Anchor not set", this);
        
        if (m_childrenBedroomLayouts == null || m_childrenBedroomLayouts.Count == 0)
            PurrLogger.LogError("Children Bedroom Layouts not set or empty", this);
        
        if (m_grandmotherBedroomAnchor == null)
            PurrLogger.LogError("Grandmother Bedroom Anchor not set", this);
        
        if (m_grandmotherBedroomLayouts == null || m_grandmotherBedroomLayouts.Count == 0)
            PurrLogger.LogError("Grandmother Bedroom Layouts not set or empty", this);
        
        if (m_bathroomAnchor == null)
            PurrLogger.LogError("Bathroom Anchor not set", this);
        
        if (m_bathroomLayouts == null || m_bathroomLayouts.Count == 0)
            PurrLogger.LogError("Bathroom Layouts not set or empty", this);
        
        if (m_dressingRoomAnchor == null)
            PurrLogger.LogError("Dressing Room Anchor not set", this);
        
        if (m_dressingRoomLayouts == null || m_dressingRoomLayouts.Count == 0)
            PurrLogger.LogError("Dressing Room Layouts not set or empty", this);
        
        if (m_pantryAnchor == null)
            PurrLogger.LogError("Pantry Anchor not set", this);
        
        if (m_pantryLayouts == null || m_pantryLayouts.Count == 0)
            PurrLogger.LogError("Pantry Layouts not set or empty", this);
        
        if (m_toilet1Anchor == null)
            PurrLogger.LogError("Toilet 1 Anchor not set", this);
        
        if (m_toilet1Layouts == null || m_toilet1Layouts.Count == 0)
            PurrLogger.LogError("Toilet 1 Layouts not set or empty", this);
        
        if (m_toilet2Anchor == null)
            PurrLogger.LogError("Toilet 2 Anchor not set", this);
        
        if (m_toilet2Layouts == null || m_toilet2Layouts.Count == 0)
            PurrLogger.LogError("Toilet 2 Layouts not set or empty", this);
        
        if (m_laundryRoomAnchor == null)
            PurrLogger.LogError("Laundry Room Anchor not set", this);
        
        if (m_laundryRoomLayouts == null || m_laundryRoomLayouts.Count == 0)
            PurrLogger.LogError("Laundry Room Layouts not set or empty", this);
        
        if (m_closet1Anchor == null)
            PurrLogger.LogError("Closet 1 Anchor not set", this);
        
        if (m_closet1Layouts == null || m_closet1Layouts.Count == 0)
            PurrLogger.LogError("Closet 1 Layouts not set or empty", this);
        
        if (m_closet2Anchor == null)
            PurrLogger.LogError("Closet 2 Anchor not set", this);
        
        if (m_closet2Layouts == null || m_closet2Layouts.Count == 0)
            PurrLogger.LogError("Closet 2 Layouts not set or empty", this);
        
        if (m_garageAnchor == null)
            PurrLogger.LogError("Garage Anchor not set", this);
        
        if (m_garageLayouts == null || m_garageLayouts.Count == 0)
            PurrLogger.LogError("Garage Layouts not set or empty", this);
        
        if (m_artWorkshopAnchor == null)
            PurrLogger.LogError("Art Workshop Anchor not set", this);
        
        if (m_artWorkshopLayouts == null || m_artWorkshopLayouts.Count == 0)
            PurrLogger.LogError("Art Workshop Layouts not set or empty", this);
        
        if (m_musicWorkshopAnchor == null)
            PurrLogger.LogError("Music Workshop Anchor not set", this);
        
        if (m_musicWorkshopLayouts == null || m_musicWorkshopLayouts.Count == 0)
            PurrLogger.LogError("Music Workshop Layouts not set or empty", this);
        
        if (m_inventionsWorkshopAnchor == null)
            PurrLogger.LogError("Inventions Workshop Anchor not set", this);
        
        if (m_inventionsWorkshopLayouts == null || m_inventionsWorkshopLayouts.Count == 0)
            PurrLogger.LogError("Inventions Workshop Layouts not set or empty", this);
        
        if (m_libraryAnchor == null)
            PurrLogger.LogError("Library Anchor not set", this);
        
        if (m_libraryLayouts == null || m_libraryLayouts.Count == 0)
            PurrLogger.LogError("Library Layouts not set or empty", this);
        
        if (m_officeAnchor == null)
            PurrLogger.LogError("Office Anchor not set", this);
        
        if (m_officeLayouts == null || m_officeLayouts.Count == 0)
            PurrLogger.LogError("Office Layouts not set or empty", this);
        
        if (m_gameRoomAnchor == null)
            PurrLogger.LogError("Game Room Anchor not set", this);
        
        if (m_gameRoomLayouts == null || m_gameRoomLayouts.Count == 0)
            PurrLogger.LogError("Game Room Layouts not set or empty", this);
    }

    private List<Room> GetLayoutsForRoomType(RoomType roomType)
    {
        switch (roomType)
        {
            case RoomType.LivingRoom: return m_livingRoomLayouts;
            case RoomType.LivingRoom2: return m_livingRoom2Layouts;
            case RoomType.DiningRoom: return m_diningRoomLayouts;
            case RoomType.Solarium: return m_solariumLayouts;
            case RoomType.ChildrenBedroom: return m_childrenBedroomLayouts;
            case RoomType.GrandmotherBedroom: return m_grandmotherBedroomLayouts;
            case RoomType.Bathroom: return m_bathroomLayouts;
            case RoomType.DressingRoom: return m_dressingRoomLayouts;
            case RoomType.Pantry: return m_pantryLayouts;
            case RoomType.Toilet1: return m_toilet1Layouts;
            case RoomType.Toilet2: return m_toilet2Layouts;
            case RoomType.LaundryRoom: return m_laundryRoomLayouts;
            case RoomType.Closet1: return m_closet1Layouts;
            case RoomType.Closet2: return m_closet2Layouts;
            case RoomType.Garage: return m_garageLayouts;
            case RoomType.ArtWorkshop: return m_artWorkshopLayouts;
            case RoomType.MusicWorkshop: return m_musicWorkshopLayouts;
            case RoomType.InventionsWorkshop: return m_inventionsWorkshopLayouts;
            case RoomType.Library: return m_libraryLayouts;
            case RoomType.Office: return m_officeLayouts;
            case RoomType.GameRoom: return m_gameRoomLayouts;
            default: return null;
        }
    }

    private Transform GetAnchorForRoomType(RoomType roomType)
    {
        switch (roomType)
        {
            case RoomType.LivingRoom: return m_livingRoomAnchor;
            case RoomType.LivingRoom2: return m_livingRoom2Anchor;
            case RoomType.DiningRoom: return m_diningRoomAnchor;
            case RoomType.Solarium: return m_solariumAnchor;
            case RoomType.ChildrenBedroom: return m_childrenBedroomAnchor;
            case RoomType.GrandmotherBedroom: return m_grandmotherBedroomAnchor;
            case RoomType.Bathroom: return m_bathroomAnchor;
            case RoomType.DressingRoom: return m_dressingRoomAnchor;
            case RoomType.Pantry: return m_pantryAnchor;
            case RoomType.Toilet1: return m_toilet1Anchor;
            case RoomType.Toilet2: return m_toilet2Anchor;
            case RoomType.LaundryRoom: return m_laundryRoomAnchor;
            case RoomType.Closet1: return m_closet1Anchor;
            case RoomType.Closet2: return m_closet2Anchor;
            case RoomType.Garage: return m_garageAnchor;
            case RoomType.ArtWorkshop: return m_artWorkshopAnchor;
            case RoomType.MusicWorkshop: return m_musicWorkshopAnchor;
            case RoomType.InventionsWorkshop: return m_inventionsWorkshopAnchor;
            case RoomType.Library: return m_libraryAnchor;
            case RoomType.Office: return m_officeAnchor;
            case RoomType.GameRoom: return m_gameRoomAnchor;
            default: return null;
        }
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
     * Big function because refactoring caused race issues
     */
    [ObserversRpc(bufferLast:true)]
    private void BuildHouse(int  _masterSeed)
    {
        
        Random.InitState(_masterSeed);
        
        
        
        if (m_livingRoomAnchor != null && m_livingRoomLayouts != null && m_livingRoomLayouts.Count > 0)
        {
            int layoutIndex = Random.Range(0, m_livingRoomLayouts.Count);
            PurrLogger.Log($"[BuildHouse] Spawning Living Room with index {layoutIndex}", this);
            Room newRoom = UnityProxy.Instantiate(GetLayoutsForRoomType(RoomType.LivingRoom)[layoutIndex], GetAnchorForRoomType(RoomType.LivingRoom));
            newRoom.PopulateRoom(m_smallPropsPercentage, m_mediumPropsPercentage, _masterSeed + 0);
        }
        
        if (m_livingRoom2Anchor != null && m_livingRoom2Layouts != null && m_livingRoom2Layouts.Count > 0)
        {
            int layoutIndex = Random.Range(0, m_livingRoom2Layouts.Count);
            PurrLogger.Log($"[BuildHouse] Spawning Living Room 2 with index {layoutIndex}", this);
            Room newRoom = UnityProxy.Instantiate(GetLayoutsForRoomType(RoomType.LivingRoom2)[layoutIndex], GetAnchorForRoomType(RoomType.LivingRoom2));
            newRoom.PopulateRoom(m_smallPropsPercentage, m_mediumPropsPercentage, _masterSeed + 1);
        }
        
        if (m_diningRoomAnchor != null && m_diningRoomLayouts != null && m_diningRoomLayouts.Count > 0)
        {
            int layoutIndex = Random.Range(0, m_diningRoomLayouts.Count);
            Room newRoom = UnityProxy.Instantiate(GetLayoutsForRoomType(RoomType.DiningRoom)[layoutIndex], GetAnchorForRoomType(RoomType.DiningRoom));
            newRoom.PopulateRoom(m_smallPropsPercentage, m_mediumPropsPercentage, _masterSeed + 2);
        }
        
        if (m_solariumAnchor != null && m_solariumLayouts != null && m_solariumLayouts.Count > 0)
        {
            int layoutIndex = Random.Range(0, m_solariumLayouts.Count);
            Room newRoom = UnityProxy.Instantiate(GetLayoutsForRoomType(RoomType.Solarium)[layoutIndex], GetAnchorForRoomType(RoomType.Solarium));
            newRoom.PopulateRoom(m_smallPropsPercentage, m_mediumPropsPercentage, _masterSeed + 3);
        }
        
        if (m_childrenBedroomAnchor != null && m_childrenBedroomLayouts != null && m_childrenBedroomLayouts.Count > 0)
        {
            int layoutIndex = Random.Range(0, m_childrenBedroomLayouts.Count);
            Room newRoom = UnityProxy.Instantiate(GetLayoutsForRoomType(RoomType.ChildrenBedroom)[layoutIndex], GetAnchorForRoomType(RoomType.ChildrenBedroom));
            newRoom.PopulateRoom(m_smallPropsPercentage, m_mediumPropsPercentage, _masterSeed + 4);
        }
        
        if (m_grandmotherBedroomAnchor != null && m_grandmotherBedroomLayouts != null && m_grandmotherBedroomLayouts.Count > 0)
        {
            int layoutIndex = Random.Range(0, m_grandmotherBedroomLayouts.Count);
            Room newRoom = UnityProxy.Instantiate(GetLayoutsForRoomType(RoomType.GrandmotherBedroom)[layoutIndex], GetAnchorForRoomType(RoomType.GrandmotherBedroom));
            newRoom.PopulateRoom(m_smallPropsPercentage, m_mediumPropsPercentage, _masterSeed + 5);
        }
        
        if (m_bathroomAnchor != null && m_bathroomLayouts != null && m_bathroomLayouts.Count > 0)
        {
            int layoutIndex = Random.Range(0, m_bathroomLayouts.Count);
            Room newRoom = UnityProxy.Instantiate(GetLayoutsForRoomType(RoomType.Bathroom)[layoutIndex], GetAnchorForRoomType(RoomType.Bathroom));
            newRoom.PopulateRoom(m_smallPropsPercentage, m_mediumPropsPercentage, _masterSeed + 6);
        }
        
        if (m_dressingRoomAnchor != null && m_dressingRoomLayouts != null && m_dressingRoomLayouts.Count > 0)
        {
            int layoutIndex = Random.Range(0, m_dressingRoomLayouts.Count);
            Room newRoom = UnityProxy.Instantiate(GetLayoutsForRoomType(RoomType.DressingRoom)[layoutIndex], GetAnchorForRoomType(RoomType.DressingRoom));
            newRoom.PopulateRoom(m_smallPropsPercentage, m_mediumPropsPercentage, _masterSeed + 7);
        }
        
        if (m_pantryAnchor != null && m_pantryLayouts != null && m_pantryLayouts.Count > 0)
        {
            int layoutIndex = Random.Range(0, m_pantryLayouts.Count);
            Room newRoom = UnityProxy.Instantiate(GetLayoutsForRoomType(RoomType.Pantry)[layoutIndex], GetAnchorForRoomType(RoomType.Pantry));
            newRoom.PopulateRoom(m_smallPropsPercentage, m_mediumPropsPercentage, _masterSeed + 8);
        }
        
        if (m_toilet1Anchor != null && m_toilet1Layouts != null && m_toilet1Layouts.Count > 0)
        {
            int layoutIndex = Random.Range(0, m_toilet1Layouts.Count);
            Room newRoom = UnityProxy.Instantiate(GetLayoutsForRoomType(RoomType.Toilet1)[layoutIndex], GetAnchorForRoomType(RoomType.Toilet1));
            newRoom.PopulateRoom(m_smallPropsPercentage, m_mediumPropsPercentage, _masterSeed + 9);
        }
        
        if (m_toilet2Anchor != null && m_toilet2Layouts != null && m_toilet2Layouts.Count > 0)
        {
            int layoutIndex = Random.Range(0, m_toilet2Layouts.Count);
            Room newRoom = UnityProxy.Instantiate(GetLayoutsForRoomType(RoomType.Toilet2)[layoutIndex], GetAnchorForRoomType(RoomType.Toilet2));
            newRoom.PopulateRoom(m_smallPropsPercentage, m_mediumPropsPercentage, _masterSeed + 10);
        }
        
        if (m_laundryRoomAnchor != null && m_laundryRoomLayouts != null && m_laundryRoomLayouts.Count > 0)
        {
            int layoutIndex = Random.Range(0, m_laundryRoomLayouts.Count);
            Room newRoom = UnityProxy.Instantiate(GetLayoutsForRoomType(RoomType.LaundryRoom)[layoutIndex], GetAnchorForRoomType(RoomType.LaundryRoom));
            newRoom.PopulateRoom(m_smallPropsPercentage, m_mediumPropsPercentage, _masterSeed + 11);
        }
        
        if (m_closet1Anchor != null && m_closet1Layouts != null && m_closet1Layouts.Count > 0)
        {
            int layoutIndex = Random.Range(0, m_closet1Layouts.Count);
            Room newRoom = UnityProxy.Instantiate(GetLayoutsForRoomType(RoomType.Closet1)[layoutIndex], GetAnchorForRoomType(RoomType.Closet1));
            newRoom.PopulateRoom(m_smallPropsPercentage, m_mediumPropsPercentage, _masterSeed + 12);
        }
        
        if (m_closet2Anchor != null && m_closet2Layouts != null && m_closet2Layouts.Count > 0)
        {
            int layoutIndex = Random.Range(0, m_closet2Layouts.Count);
            Room newRoom = UnityProxy.Instantiate(GetLayoutsForRoomType(RoomType.Closet2)[layoutIndex], GetAnchorForRoomType(RoomType.Closet2));
            newRoom.PopulateRoom(m_smallPropsPercentage, m_mediumPropsPercentage, _masterSeed + 13);
        }
        
        if (m_garageAnchor != null && m_garageLayouts != null && m_garageLayouts.Count > 0)
        {
            int layoutIndex = Random.Range(0, m_garageLayouts.Count);
            Room newRoom = UnityProxy.Instantiate(GetLayoutsForRoomType(RoomType.Garage)[layoutIndex], GetAnchorForRoomType(RoomType.Garage));
            newRoom.PopulateRoom(m_smallPropsPercentage, m_mediumPropsPercentage, _masterSeed + 14);
        }
        
        if (m_artWorkshopAnchor != null && m_artWorkshopLayouts != null && m_artWorkshopLayouts.Count > 0)
        {
            int layoutIndex = Random.Range(0, m_artWorkshopLayouts.Count);
            Room newRoom = UnityProxy.Instantiate(GetLayoutsForRoomType(RoomType.ArtWorkshop)[layoutIndex], GetAnchorForRoomType(RoomType.ArtWorkshop));
            newRoom.PopulateRoom(m_smallPropsPercentage, m_mediumPropsPercentage, _masterSeed + 15);
        }
        
        if (m_musicWorkshopAnchor != null && m_musicWorkshopLayouts != null && m_musicWorkshopLayouts.Count > 0)
        {
            int layoutIndex = Random.Range(0, m_musicWorkshopLayouts.Count);
            Room newRoom = UnityProxy.Instantiate(GetLayoutsForRoomType(RoomType.MusicWorkshop)[layoutIndex], GetAnchorForRoomType(RoomType.MusicWorkshop));
            newRoom.PopulateRoom(m_smallPropsPercentage, m_mediumPropsPercentage, _masterSeed + 16);
        }
        
        if (m_inventionsWorkshopAnchor != null && m_inventionsWorkshopLayouts != null && m_inventionsWorkshopLayouts.Count > 0)
        {
            int layoutIndex = Random.Range(0, m_inventionsWorkshopLayouts.Count);
            Room newRoom = UnityProxy.Instantiate(GetLayoutsForRoomType(RoomType.InventionsWorkshop)[layoutIndex], GetAnchorForRoomType(RoomType.InventionsWorkshop));
            newRoom.PopulateRoom(m_smallPropsPercentage, m_mediumPropsPercentage, _masterSeed + 17);
        }
        
        if (m_libraryAnchor != null && m_libraryLayouts != null && m_libraryLayouts.Count > 0)
        {
            int layoutIndex = Random.Range(0, m_libraryLayouts.Count);
            Room newRoom = UnityProxy.Instantiate(GetLayoutsForRoomType(RoomType.Library)[layoutIndex], GetAnchorForRoomType(RoomType.Library));
            newRoom.PopulateRoom(m_smallPropsPercentage, m_mediumPropsPercentage, _masterSeed + 18);
        }
        
        if (m_officeAnchor != null && m_officeLayouts != null && m_officeLayouts.Count > 0)
        {
            int layoutIndex = Random.Range(0, m_officeLayouts.Count);
            Room newRoom = UnityProxy.Instantiate(GetLayoutsForRoomType(RoomType.Office)[layoutIndex], GetAnchorForRoomType(RoomType.Office));
            newRoom.PopulateRoom(m_smallPropsPercentage, m_mediumPropsPercentage, _masterSeed + 19);
        }
        
        if (m_gameRoomAnchor != null && m_gameRoomLayouts != null && m_gameRoomLayouts.Count > 0)
        {
            int layoutIndex = Random.Range(0, m_gameRoomLayouts.Count);
            Room newRoom = UnityProxy.Instantiate(GetLayoutsForRoomType(RoomType.GameRoom)[layoutIndex], GetAnchorForRoomType(RoomType.GameRoom));
            newRoom.PopulateRoom(m_smallPropsPercentage, m_mediumPropsPercentage, _masterSeed + 20);
        }
    }

}
