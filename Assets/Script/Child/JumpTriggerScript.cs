using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/*
 * @brief Contains class declaration for JumpTriggerScript
 * @detail This script manages a list of colliders that enter and exit a trigger zone, which can be used to determine when a player or object is within a certain area for jumping mechanics.
 */
public class JumpTriggerScript : MonoBehaviour
{
    public List<Collider> m_colliders = new List<Collider>();


    private void OnTriggerEnter(Collider other)
    {
        m_colliders.Add(other);
    }

    private void OnTriggerExit(Collider other)
    {
        m_colliders.Remove(other);
    }
}
