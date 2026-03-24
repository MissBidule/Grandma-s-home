using System.Collections.Generic;
using System.Linq;
using PurrNet;
using UnityEngine;

/*
 * @brief  Contains class declaration for SabotageManager
 * @details Manages which SabotageObjects are currently sabotable.
 *          At round start, picks m_initialActiveCount objects randomly.
 *          When one is repaired, it retires it and activates a new random one.
 */
public class SabotageManager : NetworkBehaviour
{
    [SerializeField] private int m_initialActiveCount = 4;

    private List<SabotageObject> m_allObjects = new();
    private List<SabotageObject> m_activeSabotable = new();
    private List<SabotageObject> m_retired = new();

    protected override void OnSpawned()
    {
        base.OnSpawned();
        if (!isServer) return;
        Initialize();
    }

    private void Initialize()
    {
        m_allObjects = new List<SabotageObject>(FindObjectsByType<SabotageObject>(FindObjectsSortMode.None));
        m_activeSabotable.Clear();
        m_retired.Clear();

        List<SabotageObject> pool = new List<SabotageObject>(m_allObjects);
        int count = Mathf.Min(m_initialActiveCount, pool.Count);

        for (int i = 0; i < count; i++)
        {
            int idx = Random.Range(0, pool.Count);
            SabotageObject obj = pool[idx];
            pool.RemoveAt(idx);
            m_activeSabotable.Add(obj);
            obj.SetSabotable(true);
        }
    }

    /*
     * @brief Called by SabotageObject when it has been repaired.
     *        Retires the object and activates a new random one.
     */
    public void OnObjectRepaired(SabotageObject _repairedObject)
    {
        if (!isServer) return;

        m_activeSabotable.Remove(_repairedObject);
        m_retired.Add(_repairedObject);
        _repairedObject.SetSabotable(false);

        List<SabotageObject> available = m_allObjects
            .Where(o => !m_activeSabotable.Contains(o) && !m_retired.Contains(o))
            .ToList();

        if (available.Count == 0) return;

        int idx = Random.Range(0, available.Count);
        SabotageObject newObj = available[idx];
        m_activeSabotable.Add(newObj);
        newObj.SetSabotable(true);
    }
}
