using UnityEngine;


/*
@brief       Containts class declaration for PlayerController
@details     The PlayerController class handles player actions by reading input from the PlayerInputController component.
*/
public class PlayerControllerHit : MonoBehaviour
{
    private PlayerInputControllerHit m_playerInputControllerHit;

    private BoxCollider m_boxCollider;
    public bool m_isranged;


    [SerializeField] private Transform m_bulletSpawnTransform;
    [SerializeField] private GameObject m_bulletPrefab;

    private void Awake()
    {
        m_playerInputControllerHit = GetComponent<PlayerInputControllerHit>();
        m_boxCollider = GetComponent<BoxCollider>();
    }

    /*
    @brief Update triggers every frame
    Gets the movement input vector from the PlayerInputController and moves the player accordingly.
    @return void
    */
    private void Update()
    {
        Vector2 movement = m_playerInputControllerHit.m_movementInputVector;
        Vector3 moveDirection = new Vector3(movement.x, 0, movement.y);
        transform.Translate(moveDirection * Time.deltaTime * 5f);
    }

    public void Attacks()
    {
        if (m_isranged)
        {
            Shoot();
        }
        else
        {
            EnableAttack();
        }

    }

    /*
     * @brief Enables the attack collider to detect hits.
     * @return void
     */
    private void EnableAttack()
    {
        m_boxCollider.enabled = true;
    }

    /*
     * @brief Disables the attack collider to stop detecting hits.
     * @return void
     */
    public void DisableAttack()
    {
        m_boxCollider.enabled = false;
    }

    /*
     * @brief Called when the attack collider enters a trigger with another collider.
     * Triggers the hit opponent logic if the other collider belongs to a PlayerGhost.
     * @param other: The collider that was entered.
     * @return void
     */
    private void OnTriggerEnter(Collider other)
    {
        var ghost = other.GetComponent<PlayerGhost>();
        if (ghost != null)
        {
            HitOpponent();
        }
    }

    /*
     * @brief Logic executed when hitting an opponent.
     * TODO: Implement actual hit logic
     * @return void
     */
    private void HitOpponent()
    {
        print("tape un fantôme");
    }


    /*
     * @brief  This function instantiates a ball prefab
     * 
     * We instantaneously transfer the ball and put the force into impulse mode.
     */

    void Shoot()
    {
        GameObject bullet = Instantiate(m_bulletPrefab, m_bulletSpawnTransform.position, Quaternion.identity);
        bullet.GetComponent<Rigidbody>().AddForce(m_bulletSpawnTransform.forward, ForceMode.Impulse);
    }
}