using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputController : MonoBehaviour
{

    public Vector2 MovementInputVector { get; private set; }
    public void OnMove(InputAction.CallbackContext context)
    {
        MovementInputVector = context.ReadValue<Vector2>();
    }
}
