using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

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
