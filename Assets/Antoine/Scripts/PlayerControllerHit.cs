using UnityEngine;


/*
@brief       Containts class declaration for PlayerController
@details     The PlayerController class handles player actions by reading input from the PlayerInputController component.
*/
public class PlayerControllerHit : MonoBehaviour
{
    private PlayerInputControllerHit m_playerInputControllerHit;

    private BoxCollider m_boxCollider;
    private float m_hitDuration = 0.5f;
    private float m_hitTimer = 0f;


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
        if (m_playerInputControllerHit.m_isHitting)
        {
            EnableAttack();
        }
        else
        {
            DisableAttack();
        }
    }

    private void EnableAttack()
    {
        m_boxCollider.enabled = true;
    }

    private void DisableAttack()
    {
        m_boxCollider.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        var ghost = other.GetComponent<PlayerGhost>();
        if (ghost != null)
        {
            HitOpponent();
        }
    }
    private void HitOpponent()
    {
        print("tape un fantôme");
    }
}