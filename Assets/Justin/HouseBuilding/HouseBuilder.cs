using PurrNet;
using PurrNet.Logging;
using System.Collections.Generic;
using UnityEngine;

public class HouseBuilder : NetworkBehaviour
{
    /*
       Room Types:
     
       Living room (2 joined rooms)
       Dining room
       Solarium
       
       Children's bedroom
       Grandmother's bedroom
       Bathroom
       Dressing room
       
       Pantry
       Toilets (2)
       Laundry room
       Closet (Cleaning room) (2)
       Garage
       
       Art workshop
       Music workshop
       Inventions workshop
       Library
       Office
       Game room
     */
    
    
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
    [SerializeField] private float m_smallPropsPercentage;
    [SerializeField] private float m_mediumPropsPercentage;

    protected override void OnSpawned(bool _asServer)
    {
        base.OnSpawned(_asServer);
        
        enabled = _asServer;

        RoomsValidation();

        BuildHouse();
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

    private void SpawnRoom(Transform _anchor, List<Room> _layouts)
    {
        Room room;
        
        room = UnityProxy.Instantiate(_layouts[Random.Range(0, _layouts.Count)], _anchor);
        
        room.PopulateRoom(m_smallPropsPercentage, m_mediumPropsPercentage);
    }

    private void BuildHouse()
    {
        SpawnRoom(m_livingRoomAnchor, m_livingRoomLayouts);
        SpawnRoom(m_livingRoom2Anchor, m_livingRoom2Layouts);
        SpawnRoom(m_diningRoomAnchor, m_diningRoomLayouts);
        SpawnRoom(m_solariumAnchor, m_solariumLayouts);
        
        SpawnRoom(m_childrenBedroomAnchor, m_childrenBedroomLayouts);
        SpawnRoom(m_grandmotherBedroomAnchor, m_grandmotherBedroomLayouts);
        SpawnRoom(m_bathroomAnchor, m_bathroomLayouts);
        SpawnRoom(m_dressingRoomAnchor, m_dressingRoomLayouts);
        
        SpawnRoom(m_pantryAnchor, m_pantryLayouts);
        SpawnRoom(m_toilet1Anchor, m_toilet1Layouts);
        SpawnRoom(m_toilet2Anchor, m_toilet2Layouts);
        SpawnRoom(m_laundryRoomAnchor, m_laundryRoomLayouts);
        SpawnRoom(m_closet1Anchor, m_closet1Layouts);
        SpawnRoom(m_closet2Anchor, m_closet2Layouts);
        SpawnRoom(m_garageAnchor, m_garageLayouts);
        
        SpawnRoom(m_artWorkshopAnchor, m_artWorkshopLayouts);
        SpawnRoom(m_musicWorkshopAnchor, m_musicWorkshopLayouts);
        SpawnRoom(m_inventionsWorkshopAnchor, m_inventionsWorkshopLayouts);
        SpawnRoom(m_libraryAnchor, m_libraryLayouts);
        SpawnRoom(m_officeAnchor, m_officeLayouts);
        SpawnRoom(m_gameRoomAnchor, m_gameRoomLayouts);
    }

}
