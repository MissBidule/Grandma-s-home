using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private PlayerInputController _playerInputController;

    private void Awake()
    {
        _playerInputController = GetComponent<PlayerInputController>();
    }

    private void Update()
    {
        Vector2 movement = _playerInputController.MovementInputVector;
        Vector3 moveDirection = new Vector3(movement.x, 0, movement.y);
        transform.Translate(moveDirection * Time.deltaTime * 5f);
    }
}
