using PurrNet;
using UnityEngine;

public class AnchorController : NetworkBehaviour
{
    [SerializeField] private RoomController m_room;
    [SerializeField] private AnchorSize m_anchorSize;
    private GameObject m_appearance;
    


    void Start()
    {
        if (m_room == null)
        {
            m_room = GetComponentInParent<RoomController>();
        }
        GameObject appearance;
        if (m_anchorSize == AnchorSize.Small)
        {
            appearance = m_room.m_smallItemList[Random.Range(0, m_room.m_smallItemList.Length)];
        } else
        {
            appearance = m_room.m_bigItemList[Random.Range(0, m_room.m_bigItemList.Length)];
        }
        SetAppearance(appearance);
    }

    public void SetAppearance(GameObject _appearance)
    {
        if (m_appearance != null)
        {
            Destroy(m_appearance);
        }
        m_appearance = Instantiate(_appearance, transform);
    }
}
