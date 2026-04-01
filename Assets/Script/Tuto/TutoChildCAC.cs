using UnityEngine;

public class TutoChildCAC : MonoBehaviour
{
    public float m_attackRange = 3f;

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

        if (Physics.Raycast(transform.position + Vector3.up, transform.forward, out hit, m_attackRange))
        {

            TutoCAC target = hit.collider.GetComponentInParent<TutoCAC>();
            TutoFindGhost find = hit.collider.GetComponentInParent<TutoFindGhost>();

            if (target != null)
            {
                target.ShowObject();
            }
            if(find!=null)
            {
                find.FindGhost();
            }
        }
    }
}