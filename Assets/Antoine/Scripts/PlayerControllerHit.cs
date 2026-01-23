using UnityEngine;

public class PlayerControllerHit : MonoBehaviour
{
    private PlayerInputControllerHit m_playerInputController;

    private void Awake()
    {
        m_playerInputController = GetComponent<PlayerInputControllerHit>();
    }

    private void Update()
    {
        Vector2 movement = m_playerInputController.m_movementInputVector;
        Vector3 moveDirection = new Vector3(movement.x, 0, movement.y);
        transform.Translate(moveDirection * Time.deltaTime * 5f);

    }
}
