using UnityEngine;

public class TutoChildCAC : MonoBehaviour
{
    public float m_attackRange = 3f;
    //public ChildClientController childClientController;
   // private ChildClientController childClientController;

    //void Awake()
    //{
      //  childClientController = GetComponent<ChildClientController>();
    //}
    void Update()
    {
        
        if (Input.GetMouseButtonDown(0))
        //if(childClientController.m_attackPressed)
        {
            Attack();
        }
    }

    public void Attack()
    {
        Debug.Log("whyyy");
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