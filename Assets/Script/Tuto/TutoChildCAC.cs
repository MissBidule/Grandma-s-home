using UnityEngine;

public class TutoChildCAC : MonoBehaviour
{
    public float attackRange = 3f;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Attack();
        }
    }

    void Attack()
    {
        RaycastHit hit;

        if (Physics.Raycast(transform.position + Vector3.up, transform.forward, out hit, attackRange))
        {
            Debug.Log("Touché : " + hit.collider.name);

            TutoCAC target = hit.collider.GetComponentInParent<TutoCAC>();

            if (target != null)
            {
                target.ShowObject();
            }
        }
    }
}