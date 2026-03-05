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

        SeedHouse();
    }

    private List<Room> GetLayoutsForRoomType(RoomType _roomType)
    {
        switch (_roomType)
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

    private Transform GetAnchorForRoomType(RoomType _roomType)
    {
        switch (_roomType)
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
     * Big function because refactoring caused race issues ; Honestly I was just lazy before
     */
    [ObserversRpc(bufferLast:true)]
    private void BuildHouse(int  _masterSeed)
    {
        
        Random.InitState(_masterSeed);

        int masterSeedIterator = 0;
        foreach (RoomType room in System.Enum.GetValues(typeof(RoomType)))
        {
            // Double the validity check
            List<Room> roomLayouts = GetLayoutsForRoomType(room);
            Transform roomAnchor = GetAnchorForRoomType(room);

            if (roomLayouts is { Count: > 0 } && roomAnchor != null)
            {
                int layoutIndex = Random.Range(0, roomLayouts.Count);
                PurrLogger.Log($"Spawning {room.GetType()} with index {layoutIndex}", this);
                Room newRoom = UnityProxy.Instantiate(roomLayouts[layoutIndex], roomAnchor);
                newRoom.PopulateRoom(m_smallPropsPercentage, m_mediumPropsPercentage, _masterSeed + masterSeedIterator);
            }
            else
            {
                PurrLogger.Log($"{room.GetType()} Parameters invalid", this);
            }
            
            masterSeedIterator++;
        }
    }

}
