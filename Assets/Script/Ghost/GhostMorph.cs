using NUnit.Framework.Internal;
using PurrNet;
using PurrNet.Logging;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;


public class GhostMorph : NetworkBehaviour
{
    public bool m_isMorphed = false;


    [SerializeField] private GameObject m_mesh;
    [SerializeField] private GhostMorphPreview m_previewGhost;

    private GameObject m_currentPrefab = null;
    private Collider m_playerCollider;
    private MeshRenderer[] m_renderers;
    private Material[][] m_originalMaterials;

    protected override void OnSpawned()
    {
        base.OnSpawned();
    }

    void Start()
    {
        m_playerCollider = GetComponent<BoxCollider>();


        m_renderers = m_mesh.GetComponentsInChildren<MeshRenderer>();

        m_originalMaterials = new Material[m_renderers.Length][];
        for (int i = 0; i < m_renderers.Length; i++)
        {
            m_originalMaterials[i] = m_renderers[i].sharedMaterials;
        }

        // I'm so disappointed by this line that I will not even remove it as a proof of my own failure.
        if (!isServer) return; 

    }

    /*
     * @brief Copies mesh, materials, and collider from the given prefab to the player
     * Applies the components from the prefab if they exist.
     * @param _prefab: The prefab GameObject to copy from.
     * @return void
     */
    public void Morphing(GameObject _prefab, Vector3 _position)
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        MeshFilter targetFilter = _prefab.GetComponent<MeshFilter>();
        MeshRenderer targetRenderer = _prefab.GetComponent<MeshRenderer>();
        Collider targetCollider = _prefab.GetComponent<Collider>();
        if (!targetFilter || !targetRenderer || !targetCollider)
        {
            return;
        }

        InstantiateForAll(_prefab, _position);

        m_isMorphed = true;
    }

    [ObserversRpc(requireServer:true, runLocally:true)]
    public void InstantiateForAll(GameObject _prefab, Vector3 _position)
    {
        m_playerCollider.enabled = false;
        m_mesh.SetActive(false);

        m_currentPrefab = UnityProxy.InstantiateDirectly(_prefab, transform);
        m_currentPrefab.transform.localPosition = _position;
    }

    /*
     * @brief Reverts the player to their original appearance
     * Restores the original mesh, materials, and collider.
     * @return void
     */
    public void RevertToOriginal() // Server Side only (called by [ServerRCP] functions)
    {
        if (!m_isMorphed)
        {
            return;
        }

        m_isMorphed = false;
        DestroyForAll();
    }

    [ObserversRpc(requireServer: true, runLocally: true)]
    public void DestroyForAll()
    {
        m_playerCollider.enabled = true;
        m_mesh.SetActive(true);
        Destroy(m_currentPrefab);
        m_currentPrefab = null;
        for (int i = 0; i < m_renderers.Length; i++)
        {
            m_renderers[i].sharedMaterials = m_originalMaterials[i];
        }
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }
}
