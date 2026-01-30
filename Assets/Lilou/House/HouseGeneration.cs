using System.Collections.Generic;
using UnityEngine;

/**
 * @brief       Creates a new house from our room prefab
 */
public class HouseGeneration : MonoBehaviour
{
    [SerializeField] private List<GameObject> m_corners = new List<GameObject>();
    [SerializeField] private List<GameObject> m_sides = new List<GameObject>();

    private List<Vector3> m_position = new List<Vector3>
    {
        new Vector3(-1,  0, -1),
        new Vector3(-1,  0,  0),
        new Vector3(-1,  0,  1),
        new Vector3( 0,  0,  1),
        new Vector3( 1,  0,  1),
        new Vector3( 1,  0,  0),
        new Vector3( 1,  0, -1)
    };
    private List<int> m_rotation = new List<int>
    {
        0, 0,
        90, 90,
        180, 180,
        270
    };

    private List<GameObject> m_generatedRooms = new List<GameObject>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        InitHouse();
    }

    /*
     * @brief Setup the house while also deleting previous iterations
     * @return void
     */
    public void InitHouse()
    {
        //delete previous rooms
        for (int i = 0; i < m_generatedRooms.Count; i++)
        {
            Destroy(m_generatedRooms[i]);
            m_generatedRooms.RemoveAt(i--);
        }

        //prevents room duplicates
        List<GameObject> corners = new List<GameObject>(m_corners);
        List<GameObject> sides = new List<GameObject>(m_sides);
        for (int i = 0; i < m_position.Count; i++)
        {
            GameObject roomPrefab;
            if (i%2 == 0)
            {
                int index = Random.Range(0, corners.Count);
                roomPrefab = corners[index];
                corners.RemoveAt(index);
            }
            else
            {
                int index = Random.Range(0, sides.Count);
                roomPrefab = sides[index];
                sides.RemoveAt(index);
            }
            GameObject newRoom = Instantiate(roomPrefab, transform, false);
            newRoom.transform.localPosition = m_position[i];
            newRoom.transform.localRotation = Quaternion.Euler(0, m_rotation[i], 0);
            m_generatedRooms.Add(newRoom);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
