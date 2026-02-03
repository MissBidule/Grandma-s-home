using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerGhost : MonoBehaviour
{
    [Header("Transform Options")]
    [SerializeField] List<TransformOption> transformOptions;

    MeshFilter meshFilter;
    MeshRenderer meshRenderer;
    Collider currentCollider;

    void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        currentCollider = GetComponent<Collider>();
    }

    public void OnTransform(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            TransformPlayer(TransformWheelcontroller.transformID);
        }
    }

    public void TransformPlayer(int transformID)
    {
        TransformOption option = transformOptions.Find(o => o.id == transformID);

        if (option == null || option.prefab == null)
        {
            return;
        }

        CopyFromPrefab(option.prefab);
    }

    void CopyFromPrefab(GameObject prefab)
    {
        MeshFilter targetFilter = prefab.GetComponent<MeshFilter>();
        MeshRenderer targetRenderer = prefab.GetComponent<MeshRenderer>();
        Collider targetCollider = prefab.GetComponent<Collider>();

        if (!targetFilter || !targetRenderer || !targetCollider)
        {
            return;
        }

        meshFilter.mesh = targetFilter.sharedMesh;
        meshRenderer.materials = targetRenderer.sharedMaterials;

        ReplaceCollider(targetCollider);
    }

    void ReplaceCollider(Collider target)
    {
        if (currentCollider != null)
            Destroy(currentCollider);

        System.Type type = target.GetType();
        currentCollider = gameObject.AddComponent(type) as Collider;

        if (currentCollider is BoxCollider box &&
            target is BoxCollider tBox)
        {
            box.center = tBox.center;
            box.size = tBox.size;
        }
        else if (currentCollider is SphereCollider sphere &&
                 target is SphereCollider tSphere)
        {
            sphere.center = tSphere.center;
            sphere.radius = tSphere.radius;
        }
        else if (currentCollider is CapsuleCollider capsule &&
                 target is CapsuleCollider tCapsule)
        {
            capsule.center = tCapsule.center;
            capsule.radius = tCapsule.radius;
            capsule.height = tCapsule.height;
            capsule.direction = tCapsule.direction;
        }
    }
}
