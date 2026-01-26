using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.InputSystem;

/*
@brief       Contains class declaration for PlayerGhost
@details     The PlayerGhost class allows the player to interact with items to sabotage them and transform into different objects.
*/

public class PlayerGhost : MonoBehaviour
{
    [Header("Transform Options")]
    [SerializeField] List<TransformOption> m_transformOptions;

    MeshFilter m_meshFilter;
    MeshRenderer m_meshRenderer;
    Collider m_currentCollider;

    void Awake()
    {
        m_meshFilter = GetComponent<MeshFilter>();
        m_meshRenderer = GetComponent<MeshRenderer>();
        m_currentCollider = GetComponent<Collider>();
    }

    /*
    @brief Gets the transform input action
    @return void
    */
    public void OnTransform(InputAction.CallbackContext _context)
    {
        if (_context.performed)
        {
            TransformPlayer(TransformWheelcontroller.m_transformID);
        }
    }

    /*
     * @brief Transforms the player into the selected object.
     * Finds the corresponding TransformOption by ID and triggers the transformation method by giving a reference to the game object.
     * @param transformID: The ID of the transformation option selected.
     * @return void
     */
    public void TransformPlayer(int _transformID)
    {
        TransformOption option = m_transformOptions.Find(o => o.id == _transformID);

        if (option == null || option.prefab == null)
        {
            return;
        }

        CopyFromPrefab(option.prefab);
    }

    /*
     * @brief Copies mesh, materials, and collider from the given prefab to the player.
     * @param _prefab: The prefab GameObject to copy from.
     * @return void
     */
    void CopyFromPrefab(GameObject _prefab)
    {
        MeshFilter targetFilter = _prefab.GetComponent<MeshFilter>();
        MeshRenderer targetRenderer = _prefab.GetComponent<MeshRenderer>();
        Collider targetCollider = _prefab.GetComponent<Collider>();

        if (!targetFilter || !targetRenderer || !targetCollider)
        {
            return;
        }

        m_meshFilter.mesh = targetFilter.sharedMesh;
        m_meshRenderer.materials = targetRenderer.sharedMaterials;

        ReplaceCollider(targetCollider);
    }

    /*
     * @brief Replaces the current collider with a new one based on the target collider.
     * Copies relevant properties from the target collider to the new collider.
     * @param _target: The target Collider to copy from.
     * @return void
     */
    void ReplaceCollider(Collider _target)
    {
        if (m_currentCollider != null)
            Destroy(m_currentCollider);

        System.Type type = _target.GetType();
        m_currentCollider = gameObject.AddComponent(type) as Collider;

        if (m_currentCollider is BoxCollider box &&
            _target is BoxCollider tBox)
        {
            box.center = tBox.center;
            box.size = tBox.size;
        }
        else if (m_currentCollider is SphereCollider sphere &&
                 _target is SphereCollider tSphere)
        {
            sphere.center = tSphere.center;
            sphere.radius = tSphere.radius;
        }
        else if (m_currentCollider is CapsuleCollider capsule &&
                 _target is CapsuleCollider tCapsule)
        {
            capsule.center = tCapsule.center;
            capsule.radius = tCapsule.radius;
            capsule.height = tCapsule.height;
            capsule.direction = tCapsule.direction;
        }
    }
}
